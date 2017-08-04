using Hagar.WireProtocol;

namespace Hagar.ObjectModel
{
    public class EndObject : IToken
    {
        private readonly Field field;

        public EndObject(Field field)
        {
            this.field = field;
        }

        public override string ToString()
        {
            return $"EndObject({nameof(this.field)}: {this.field})";
        }
    }
}