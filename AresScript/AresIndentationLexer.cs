using Antlr4.Runtime;
using AresScript.Generated;

namespace AresScript;
/// <summary>
/// Custom lexer to handle indentation-based blocks so that the language is more similar
/// to Python which more people might be familiar with as opposed to using the curlies.
/// It generates INDENT and DEDENT tokens based on leading whitespace.
/// </summary>
public class AresIndentationLexer : AresLangLexer
{
  private const int IndentToken = AresLangParser.INDENT;
  private const int DedentToken = AresLangParser.DEDENT;

  // A queue to hold tokens we generate (INDENT/DEDENT) before returning the real token
  private readonly Queue<IToken> _pendingTokens = new();

  // A stack to keep track of indentation levels (starts with 0)
  private readonly Stack<int> _indents = new();

  // Keep track of the previous token to know if we just saw a newline
  private IToken? _lastToken = null;

  public AresIndentationLexer(ICharStream input) : base(input)
  {
    _indents.Push(0);
  }

  public override IToken NextToken()
  {
    // 1. If we have queued tokens (INDENTs/DEDENTs), return them first
    if (_pendingTokens.Count > 0)
    {
      var next = _pendingTokens.Dequeue();
      _lastToken = next;
      return next;
    }

    // 2. Get the next raw token from the ANTLR lexer
    IToken token = base.NextToken();

    // 3. Check indentation if we are at the start of a line
    // (i.e., if the previous token was a NEWLINE or we are at the start of the file)
    // We ignore EOF and NEWLINE itself (empty lines don't change indentation)
    if ((_lastToken == null || _lastToken.Type == NEWLINE) &&
        token.Type != Eof &&
        token.Type != NEWLINE)
    {
      int currentIndent = token.Column;
      int previousIndent = _indents.Peek();

      if (currentIndent > previousIndent)
      {
        // Indentation increased -> Emit INDENT
        _indents.Push(currentIndent);
        _pendingTokens.Enqueue(CreateToken(IndentToken, token));
      }
      else if (currentIndent < previousIndent)
      {
        // Indentation decreased -> Emit one or more DEDENTs
        while (currentIndent < _indents.Peek())
        {
          _indents.Pop();
          _pendingTokens.Enqueue(CreateToken(DedentToken, token));
        }

        // Safety check: ensure we landed on a valid previous indentation level
        if (currentIndent != _indents.Peek())
        {
          // You might want to throw a custom error here for "Unaligned dedent"
        }
      }
    }

    // 4. Handle End Of File: Close any remaining open blocks
    if (token.Type == Eof && _indents.Count > 1)
    {
      // This one's here to make it work when there's a top level block without a newline at the end
      // we just add a "fake" newline to make sure we can still run the block without syntax errors
      if (_lastToken is not null && _lastToken.Type != NEWLINE)
      {
        _pendingTokens.Enqueue(CreateToken(NEWLINE, token));
      }

      while (_indents.Count > 1)
      {
        _indents.Pop();
        _pendingTokens.Enqueue(CreateToken(DedentToken, token));
      }
      _pendingTokens.Enqueue(token); // Ensure EOF is the very last token
      var next = _pendingTokens.Dequeue();
      _lastToken = next;
      return next;
    }

    // 5. If we generated tokens, queue the real token and return the first generated one
    if (_pendingTokens.Count > 0)
    {
      _pendingTokens.Enqueue(token);
      var next = _pendingTokens.Dequeue();
      _lastToken = next;
      return next;
    }

    _lastToken = token;

    // Otherwise, just return the real token
    return token;
  }

  // Helper to create a token with the correct line/column info
  private static CommonToken CreateToken(int type, IToken source)
  {
    var token = new CommonToken(type)
    {
      Line = source.Line,
      Column = source.Column,
      StartIndex = source.StartIndex,
      StopIndex = source.StartIndex - 1
    };
    return token;
  }
}
