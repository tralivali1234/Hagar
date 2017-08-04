using System.Collections.Generic;
using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.ObjectModel
{
    public class Parser
    {
        public IEnumerable<IToken> Parse(Reader reader, SerializationContext context)
        {
            var depth = 0;
            do
            {
                var field = reader.ReadFieldHeader(context);
                if (field.IsEndObject)
                {
                    depth--;
                    yield return new EndObject(field);
                }
                else if (field.IsStartObject)
                {
                    depth++;
                    yield return new StartObject(field);
                }
                else if (field.HasFieldId)
                {

                }

            } while (depth > 0);
            yield break;
        }
    }
}
