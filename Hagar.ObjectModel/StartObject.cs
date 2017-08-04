using Hagar.WireProtocol;

namespace Hagar.ObjectModel
{
    public class StartObject : IToken
    {
        private readonly Field field;

        public StartObject(Field field)
        {
            this.field = field;
        }

        public override string ToString()
        {
            return $"StartObject({nameof(this.field)}: {this.field})";
        }
    }
}