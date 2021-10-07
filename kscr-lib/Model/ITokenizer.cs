using System.Collections.Generic;

namespace KScr.Lib.Model
{
    public interface ITokenizer
    {
        IList<Token> Tokenize(string source);
    }
}