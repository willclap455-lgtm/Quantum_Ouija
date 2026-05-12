using System.Text;

namespace QuantumOuija.Input;

public sealed class TextInputBuffer
{
    private readonly StringBuilder _builder = new();

    public TextInputBuffer(int maxLength)
    {
        MaxLength = maxLength;
    }

    public int MaxLength { get; }
    public string Text => _builder.ToString();

    public void Append(char character)
    {
        if (_builder.Length >= MaxLength || char.IsControl(character))
        {
            return;
        }

        _builder.Append(character);
    }

    public void Backspace()
    {
        if (_builder.Length > 0)
        {
            _builder.Remove(_builder.Length - 1, 1);
        }
    }

    public void Clear() => _builder.Clear();
}
