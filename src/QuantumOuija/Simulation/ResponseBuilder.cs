using System.Text;
using QuantumOuija.Board;

namespace QuantumOuija.Simulation;

public sealed class ResponseBuilder
{
    private readonly StringBuilder _response = new();

    public string Text => _response.ToString().TrimStart();

    public ResponseAppendResult Append(BoardToken token, bool isFirstPath)
    {
        if (isFirstPath && token.IsTerminator)
        {
            _response.Clear();
            _response.Append(token.Value);
            return new ResponseAppendResult(token.Value, terminateResponse: true);
        }

        var text = token.Type switch
        {
            RegionType.Letter or RegionType.Number => token.Value,
            RegionType.Yes or RegionType.No or RegionType.Goodbye => " ",
            RegionType.Space or RegionType.Empty => " ",
            _ => " "
        };

        AppendCollapsingSpaces(text);
        return new ResponseAppendResult(text, terminateResponse: false);
    }

    private void AppendCollapsingSpaces(string text)
    {
        if (text == " ")
        {
            if (_response.Length > 0 && _response[^1] != ' ')
            {
                _response.Append(' ');
            }

            return;
        }

        _response.Append(text);
    }
}

public readonly record struct ResponseAppendResult(string TextAppended, bool TerminateResponse);
