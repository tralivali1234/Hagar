using System;
using System.Collections.Generic;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class DictionaryCodec<TKey, TValue> : IFieldCodec<Dictionary<TKey, TValue>>
    {
        private readonly IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec;
        private readonly IFieldCodec<int> intCodec;
        private readonly IUntypedCodecProvider codecProvider;
        private readonly IFieldCodec<IEqualityComparer<TKey>> ecCodec;
        private readonly IActivator<ValueTuple<IEqualityComparer<TKey>, int>, Dictionary<TKey, TValue>> activator;

        public DictionaryCodec(
            IFieldCodec<KeyValuePair<TKey, TValue>> pairCodec,
            IFieldCodec<int> intCodec,
            IUntypedCodecProvider codecProvider,
            IFieldCodec<IEqualityComparer<TKey>> ecCodec,
            IActivator<ValueTuple<IEqualityComparer<TKey>, int>, Dictionary<TKey, TValue>> activator)
        {
            this.pairCodec = pairCodec;
            this.intCodec = intCodec;
            this.codecProvider = codecProvider;
            this.ecCodec = ecCodec;
            this.activator = activator;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, Dictionary<TKey, TValue> value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);
            
            if (value.Comparer != EqualityComparer<TKey>.Default)
            {
                this.ecCodec.WriteField(writer, session, 0, typeof(IEqualityComparer<TKey>), value.Comparer);
            }

            this.intCodec.WriteField(writer, session, 1, typeof(int), value.Count);

            var first = true;
            foreach (var element in value)
            {
                this.pairCodec.WriteField(writer, session, first ? 1U : 0, typeof(KeyValuePair<TKey, TValue>), element);
                first = false;
            }

            writer.WriteEndObject();
        }

        public Dictionary<TKey, TValue> ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<Dictionary<TKey, TValue>>(reader, session, field, this.codecProvider);
            if (field.WireType != WireType.TagDelimited) ThrowUnsupportedWireTypeException(field);

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(session);
            Dictionary<TKey, TValue> result = null;
            IEqualityComparer<TKey> comparer = null;
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        comparer = this.ecCodec.ReadValue(reader, session, header);
                        break;
                    case 1:
                        var size = this.intCodec.ReadValue(reader, session, header);
                        result = this.activator.Create(ValueTuple.Create(comparer ?? EqualityComparer<TKey>.Default, size));
                        ReferenceCodec.RecordObject(session, result, placeholderReferenceId);
                        break;
                    case 2:
                        if (result == null) ThrowLengthFieldMissing();
                        var pair = this.pairCodec.ReadValue(reader, session, header);
                        // ReSharper disable once PossibleNullReferenceException
                        result.Add(pair.Key, pair.Value);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }
            
            return result;
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.TagDelimited} is supported. {field}");

        private static void ThrowLengthFieldMissing() => throw new RequiredFieldMissingException("Serialized object is missing its length field.");
    }
}
