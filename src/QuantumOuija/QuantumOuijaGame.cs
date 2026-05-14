using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using QuantumOuija.Audio;
using QuantumOuija.Board;
using QuantumOuija.Input;
using QuantumOuija.Persistence;
using QuantumOuija.Rendering;
using QuantumOuija.Rng;
using QuantumOuija.Simulation;

namespace QuantumOuija;

public sealed class QuantumOuijaGame : Game
{
    private const int TopUiHeight = 82;
    private const int BottomUiHeight = 118;
    private const float PlanchetteBoardSizeRatio = 0.155f;
    private const float BackspaceInitialRepeatDelaySeconds = 0.35f;
    private const float BackspaceRepeatIntervalSeconds = 0.045f;

    private readonly GameOptions _options;
    private readonly GraphicsDeviceManager _graphics;
    private readonly BoardRenderer _boardRenderer = new();
    private readonly BitmapTextRenderer _textRenderer = new();
    private readonly TrailRenderer _trailRenderer = new();
    private readonly TextInputBuffer _questionInput = new(180);
    private readonly AudioHooks _audio = new();
    private readonly List<GeneratedPath> _sessionPaths = new();

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _boardTexture = null!;
    private Texture2D _planchetteTexture = null!;
    private BoardModel _board = null!;
    private BoardViewTransform _transform = null!;
    private IQuantumRandomProvider _randomProvider = null!;
    private QuantumPathGenerator _pathGenerator = null!;
    private PlanchetteAnimator _animator = null!;
    private ResponseBuilder _responseBuilder = new();
    private KeyboardState _previousKeyboard;
    private CancellationTokenSource? _sessionCancellation;
    private Task<int>? _pathCountTask;
    private Task<GeneratedPath>? _pathTask;
    private GeneratedPath? _currentPath;
    private GridNode _currentNode;
    private float _backspaceHeldSeconds;
    private float _backspaceRepeatSeconds;
    private SimulationState _state = SimulationState.Idle;
    private int _requestedPathCount;
    private int _completedPathCount;
    private float _pauseRemaining;
    private string _status = "Type a question, then press ENTER.";
    private string? _error;
    private SessionRecord? _lastSessionRecord;

    public QuantumOuijaGame(GameOptions options)
    {
        _options = options;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = options.WindowWidth,
            PreferredBackBufferHeight = options.WindowHeight,
            SynchronizeWithVerticalRetrace = true
        };

        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Quantum Ouija";
        Window.AllowUserResizing = true;
        Window.TextInput += OnTextInput;
    }

    protected override void Initialize()
    {
        var fallback = new FallbackRandomProvider();
        _randomProvider = new CurbyQuantumRandomProvider(
            new CurbyClientOptions
            {
                BaseUri = new Uri(_options.CurbyBaseUri),
                ChainId = _options.CurbyChainId
            },
            fallback);
        _pathGenerator = new QuantumPathGenerator(
            _randomProvider,
            new GridMovementEngine(new MovementOptions { WrapAtBoardEdges = _options.WrapMovement }));

        _audio.StartAmbientLoop();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _boardTexture = LoadTexture("Content/ouija_board.jpg");
        _planchetteTexture = LoadTexture("Content/planchette.png");
        _board = MiltonBradleyBoardLayout.Create(_boardTexture.Width, _boardTexture.Height, _options.GridSpacingPixels);
        _transform = BoardViewTransform.Fit(
            _board.Width,
            _board.Height,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            TopUiHeight,
            BottomUiHeight);

        _currentNode = _board.CenterNode;
        _animator = new PlanchetteAnimator(
            _board.NodeToWorld(_currentNode),
            _options.PlanchetteSpeedPixelsPerSecond,
            _options.WobblePlanchette);
    }

    protected override void UnloadContent()
    {
        _sessionCancellation?.Cancel();
        _sessionCancellation?.Dispose();
        if (_randomProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _audio.StopAmbientLoop();
        Window.TextInput -= OnTextInput;
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        HandleKeyboard(keyboard, gameTime);
        UpdateSession(gameTime);
        _animator.Update(gameTime);
        _trailRenderer.Update(gameTime, _animator.CurrentPosition, _animator.IsRunning);
        Window.Title = $"Quantum Ouija - {_state} - {_responseBuilder.Text}";
        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(10, 8, 12));
        _transform = BoardViewTransform.Fit(
            _board.Width,
            _board.Height,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            TopUiHeight,
            BottomUiHeight);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);

        _boardRenderer.Draw(
            _spriteBatch,
            _boardTexture,
            _pixel,
            _board,
            _transform,
            _options.ShowDebugGrid,
            _options.ShowDebugRegions);

        _trailRenderer.Draw(_spriteBatch, _pixel, _transform);

        if (_options.ShowDebugNodes && _currentPath is not null)
        {
            DrawCurrentPathNodes();
        }

        DrawPlanchette();
        DrawUi();

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private Texture2D LoadTexture(string relativePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        using var stream = File.OpenRead(fullPath);
        return Texture2D.FromStream(GraphicsDevice, stream);
    }

    private void HandleKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (WasPressed(Keys.F1, keyboard))
        {
            _options.ShowDebugGrid = !_options.ShowDebugGrid;
        }

        if (WasPressed(Keys.F2, keyboard))
        {
            _options.ShowDebugRegions = !_options.ShowDebugRegions;
        }

        if (WasPressed(Keys.F3, keyboard))
        {
            _options.ShowDebugNodes = !_options.ShowDebugNodes;
        }

        HandleQuestionEditingKeys(keyboard, (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (WasPressed(Keys.Enter, keyboard) && CanStartSession)
        {
            StartSession();
        }

        if (WasPressed(Keys.Escape, keyboard))
        {
            if (IsSessionActive)
            {
                CancelSession();
            }
            else
            {
                Exit();
            }
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (CanEditQuestion)
        {
            _questionInput.Append(e.Character);
        }
    }

    private void HandleQuestionEditingKeys(KeyboardState keyboard, float elapsedSeconds)
    {
        if (!CanEditQuestion)
        {
            ResetBackspaceRepeat();
            return;
        }

        if (IsQuestionClearHotkeyDown(keyboard))
        {
            if (!IsQuestionClearHotkeyDown(_previousKeyboard))
            {
                _questionInput.Clear();
            }

            ResetBackspaceRepeat();
            return;
        }

        if (!keyboard.IsKeyDown(Keys.Back))
        {
            ResetBackspaceRepeat();
            return;
        }

        if (WasPressed(Keys.Back, keyboard))
        {
            _questionInput.Backspace();
            _backspaceHeldSeconds = 0f;
            _backspaceRepeatSeconds = 0f;
            return;
        }

        var previousHeldSeconds = _backspaceHeldSeconds;
        _backspaceHeldSeconds += elapsedSeconds;
        if (_backspaceHeldSeconds < BackspaceInitialRepeatDelaySeconds)
        {
            return;
        }

        _backspaceRepeatSeconds += elapsedSeconds;
        if (previousHeldSeconds < BackspaceInitialRepeatDelaySeconds)
        {
            _backspaceRepeatSeconds += BackspaceRepeatIntervalSeconds;
        }

        while (_backspaceRepeatSeconds >= BackspaceRepeatIntervalSeconds)
        {
            _questionInput.Backspace();
            _backspaceRepeatSeconds -= BackspaceRepeatIntervalSeconds;
        }
    }

    private void ResetBackspaceRepeat()
    {
        _backspaceHeldSeconds = 0f;
        _backspaceRepeatSeconds = 0f;
    }

    private void StartSession()
    {
        _sessionCancellation?.Dispose();
        _sessionCancellation = new CancellationTokenSource();
        _responseBuilder = new ResponseBuilder();
        _sessionPaths.Clear();
        _lastSessionRecord = null;
        _error = null;
        _requestedPathCount = 0;
        _completedPathCount = 0;
        _currentPath = null;
        _trailRenderer.Clear();
        _currentNode = _board.CenterNode;
        _animator.StartPath(new[] { _board.NodeToWorld(_currentNode) });
        _pathCountTask = _pathGenerator.GeneratePathCountAsync(_sessionCancellation.Token);
        _pathTask = null;
        _state = SimulationState.GeneratingPathCount;
        _status = "Contacting CURBy quantum beacon for path count...";
    }

    private void CancelSession()
    {
        _sessionCancellation?.Cancel();
        _state = SimulationState.Cancelled;
        _status = "Session cancelled.";
    }

    private void UpdateSession(GameTime gameTime)
    {
        switch (_state)
        {
            case SimulationState.GeneratingPathCount:
                CompletePathCountIfReady();
                break;
            case SimulationState.GeneratingPath:
                CompletePathGenerationIfReady();
                break;
            case SimulationState.AnimatingPath:
                CompleteAnimationIfReady();
                break;
            case SimulationState.PausingBetweenPaths:
                _pauseRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_pauseRemaining <= 0)
                {
                    BeginNextPathGeneration();
                }

                break;
        }
    }

    private void CompletePathCountIfReady()
    {
        if (_pathCountTask is null || !_pathCountTask.IsCompleted)
        {
            return;
        }

        if (!TryReadTask(_pathCountTask, out _requestedPathCount))
        {
            return;
        }

        _status = $"Quantum path count: {_requestedPathCount}. Generating first path...";
        BeginNextPathGeneration();
    }

    private void BeginNextPathGeneration()
    {
        _state = SimulationState.GeneratingPath;
        _currentNode = _board.CenterNode;
        _pathTask = _pathGenerator.GeneratePathAsync(_completedPathCount, _board, _currentNode, _sessionCancellation!.Token);
        _status = $"Generating path {_completedPathCount + 1}/{_requestedPathCount}...";
    }

    private void CompletePathGenerationIfReady()
    {
        if (_pathTask is null || !_pathTask.IsCompleted)
        {
            return;
        }

        if (!TryReadTask(_pathTask, out var path))
        {
            return;
        }

        _currentPath = path;
        var waypoints = path.Nodes.Select(node => _board.NodeToWorld(node)).ToArray();
        _animator.StartPath(waypoints);
        _audio.OnPathStarted();
        _state = SimulationState.AnimatingPath;
        _status = $"Animating path {path.Index + 1}/{_requestedPathCount}: final token will resolve at rest.";
    }

    private void CompleteAnimationIfReady()
    {
        if (!_animator.CompletedThisFrame || _currentPath is null)
        {
            return;
        }

        var resolvedToken = _board.ResolveToken(GetPlanchetteTipPosition(_animator.CurrentPosition));
        _currentPath = _currentPath with { FinalToken = resolvedToken };
        _currentNode = _currentPath.EndNode;
        _sessionPaths.Add(_currentPath);
        var result = _responseBuilder.Append(resolvedToken, _completedPathCount == 0);
        _audio.OnTokenResolved(resolvedToken.ToString());
        _completedPathCount++;

        if (result.TerminateResponse || _completedPathCount >= _requestedPathCount)
        {
            CompleteSession();
            return;
        }

        _pauseRemaining = _options.PauseBetweenPathsSeconds;
        _state = SimulationState.PausingBetweenPaths;
        _status = $"Resolved {resolvedToken}; response now: {_responseBuilder.Text}";
    }

    private void CompleteSession()
    {
        _state = SimulationState.Complete;
        _status = "Response complete. Edit the question or press ENTER to ask again.";
        _lastSessionRecord = SessionRecordFactory.Create(
            _questionInput.Text,
            _responseBuilder.Text,
            _requestedPathCount,
            _sessionPaths,
            _randomProvider.Name,
            _randomProvider.IsDeterministic);
    }

    private bool TryReadTask<T>(Task<T> task, out T value)
    {
        value = default!;

        if (task.IsCanceled)
        {
            _state = SimulationState.Cancelled;
            _status = "Session cancelled.";
            return false;
        }

        if (task.IsFaulted)
        {
            _state = SimulationState.Error;
            _error = task.Exception?.GetBaseException().Message ?? "Unknown simulation error.";
            _status = _error;
            return false;
        }

        value = task.GetAwaiter().GetResult();
        return true;
    }

    private void DrawPlanchette()
    {
        var screen = _transform.BoardToScreen(_animator.RenderPosition);
        var size = GetPlanchetteBoardSize() * _transform.Scale;
        var destination = new Rectangle(
            (int)(screen.X - size * 0.5f),
            (int)(screen.Y - size * 0.5f),
            (int)size,
            (int)size);

        _spriteBatch.Draw(_planchetteTexture, destination, Color.White * 0.92f);
    }

    private float GetPlanchetteBoardSize() =>
        MathF.Min(_board.Width, _board.Height) * PlanchetteBoardSizeRatio;

    private Vector2 GetPlanchetteTipPosition(Vector2 center) =>
        center + new Vector2(0, GetPlanchetteBoardSize() * -0.5f);

    private void DrawCurrentPathNodes()
    {
        var stride = Math.Max(1, _currentPath!.Nodes.Count / 400);
        for (var i = 0; i < _currentPath.Nodes.Count; i += stride)
        {
            var screen = _transform.BoardToScreen(_board.NodeToWorld(_currentPath.Nodes[i]));
            _spriteBatch.Draw(_pixel, new Rectangle((int)screen.X - 1, (int)screen.Y - 1, 2, 2), Color.Yellow * 0.55f);
        }
    }

    private void DrawUi()
    {
        var viewport = GraphicsDevice.Viewport;
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, TopUiHeight), new Color(6, 4, 9, 225));
        _spriteBatch.Draw(_pixel, new Rectangle(24, 18, viewport.Width - 48, 46), new Color(18, 14, 24, 235));

        var panelTop = viewport.Height - BottomUiHeight;
        _spriteBatch.Draw(_pixel, new Rectangle(0, panelTop, viewport.Width, BottomUiHeight), new Color(6, 4, 9, 225));
        _spriteBatch.Draw(_pixel, new Rectangle(24, panelTop + 18, viewport.Width - 48, 46), new Color(25, 20, 32, 235));

        var question = CanEditQuestion ? _questionInput.Text + "_" : _questionInput.Text;
        var response = string.IsNullOrWhiteSpace(_responseBuilder.Text) ? "..." : _responseBuilder.Text;
        var debug = $"STATE {_state}  PATHS {_completedPathCount}/{_requestedPathCount}  RNG {_randomProvider.Name}  CTRL U CLEAR  F1 GRID F2 REGIONS F3 NODES ESC CANCEL/QUIT";

        _textRenderer.DrawText(_spriteBatch, _pixel, "RESPONSE: " + response, new Vector2(36, 30), new Color(166, 238, 210), 2, viewport.Width - 72);
        _textRenderer.DrawText(_spriteBatch, _pixel, "QUESTION: " + question, new Vector2(36, panelTop + 30), new Color(215, 205, 180), 2, viewport.Width - 72);
        _textRenderer.DrawText(_spriteBatch, _pixel, _error ?? _status, new Vector2(36, panelTop + 78), new Color(185, 150, 205), 1, viewport.Width - 72);
        _textRenderer.DrawText(_spriteBatch, _pixel, debug, new Vector2(36, panelTop + 96), new Color(112, 96, 125), 1, viewport.Width - 72);
    }

    private bool WasPressed(Keys key, KeyboardState keyboard) =>
        keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private static bool IsQuestionClearHotkeyDown(KeyboardState keyboard)
    {
        var isControlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        return isControlDown && (keyboard.IsKeyDown(Keys.U) || keyboard.IsKeyDown(Keys.Back));
    }

    private bool CanEditQuestion =>
        _state is SimulationState.Idle or SimulationState.Complete or SimulationState.Cancelled or SimulationState.Error;

    private bool CanStartSession => CanEditQuestion && !string.IsNullOrWhiteSpace(_questionInput.Text);

    private bool IsSessionActive =>
        _state is SimulationState.GeneratingPathCount or SimulationState.GeneratingPath or SimulationState.AnimatingPath or SimulationState.PausingBetweenPaths;
}
