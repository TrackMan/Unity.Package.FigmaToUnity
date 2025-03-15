using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.IO;
using UnityEngine;

namespace Figma.Internals
{
    public class JsonUtility
    {
        class ArrayPool : IArrayPool<char>
        {
            #region Methods
            public char[] Rent(int minimumLength) => ArrayPool<char>.Shared.Rent(minimumLength);
            public void Return(char[] array) => ArrayPool<char>.Shared.Return(array);
            #endregion
        }

        #region Properties
        static JsonSerializer serializer = new();

        static readonly IArrayPool<char> arrayPool = new ArrayPool();
        #endregion

        #region Constructors
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            JsonSerializerSettings settings = new()
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters =
                {
                    new EffectArrayConverter(),
                    new PaintArrayConverter(),
                    new LayoutGridArrayConverter(),
                    new ExportSettingsArrayConverter(),
                    new TransitionConverter(),
                    new BaseNodeArrayConverter(),
                    new SceneNodeArrayConverter()
                }
            };
            serializer = JsonSerializer.Create(settings);
        }
        public static string ToJson<T>(T value, bool prettyPrint)
        {
            using StringWriter stringWriter = new();
            using JsonTextWriter jsonTextWriter = new(stringWriter) { Formatting = prettyPrint ? Formatting.Indented : Formatting.None };
            serializer.Serialize(jsonTextWriter, value);
            return stringWriter.ToString();
        }
        public static T FromJson<T>(string json, bool useArrayPool = true)
        {
            using StringReader stringReader = new(json);
            using JsonTextReader jsonTextReader = new(stringReader);
            if (useArrayPool)
                jsonTextReader.ArrayPool = arrayPool;

            return serializer.Deserialize<T>(jsonTextReader);
        }
        #endregion
    }

    public abstract class ArrayConverter<T, TEnum> : JsonConverter
    {
        #region Methods
        public override bool CanConvert(Type objectType) => objectType == typeof(T[]);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            T[] result = new T[array.Count];

            for (int i = 0; i < array.Count; ++i)
                result[i] = ToObject((JObject)array[i], serializer);

            return result;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            T[] array = (T[])value;
            writer.WriteStartArray();

            foreach (T node in array)
                serializer.Serialize(writer, node);

            writer.WriteEndArray();
        }
        protected TEnum GetValue(JObject obj, string name = "type") => (TEnum)Enum.Parse(typeof(TEnum), obj[name].Value<string>());
        protected abstract T ToObject(JObject obj, JsonSerializer serializer);
        #endregion
    }

    public class EffectArrayConverter : ArrayConverter<Effect, EffectType>
    {
        #region Methods
        protected override Effect ToObject(JObject obj, JsonSerializer serializer) =>
            GetValue(obj) switch
            {
                EffectType.INNER_SHADOW => obj.ToObject<ShadowEffect>(serializer),
                EffectType.DROP_SHADOW => obj.ToObject<ShadowEffect>(serializer),
                EffectType.LAYER_BLUR => obj.ToObject<BlurEffect>(serializer),
                EffectType.BACKGROUND_BLUR => obj.ToObject<BlurEffect>(serializer),
                _ => throw new NotSupportedException()
            };
        #endregion
    }

    public class PaintArrayConverter : ArrayConverter<Paint, PaintType>
    {
        #region Methods
        protected override Paint ToObject(JObject obj, JsonSerializer serializer) =>
            GetValue(obj) switch
            {
                PaintType.SOLID => obj.ToObject<SolidPaint>(serializer),
                PaintType.GRADIENT_LINEAR => obj.ToObject<GradientPaint>(serializer),
                PaintType.GRADIENT_RADIAL => obj.ToObject<GradientPaint>(serializer),
                PaintType.GRADIENT_ANGULAR => obj.ToObject<GradientPaint>(serializer),
                PaintType.GRADIENT_DIAMOND => obj.ToObject<GradientPaint>(serializer),
                PaintType.IMAGE => obj.ToObject<ImagePaint>(serializer),
                PaintType.EMOJI => obj.ToObject<ImagePaint>(serializer),
                _ => throw new NotSupportedException()
            };
        #endregion
    }

    public class LayoutGridArrayConverter : ArrayConverter<LayoutGrid, Pattern>
    {
        #region Methods
        protected override LayoutGrid ToObject(JObject obj, JsonSerializer serializer) =>
            GetValue(obj, "pattern") switch
            {
                Pattern.COLUMNS => obj.ToObject<RowsColsLayoutGrid>(serializer),
                Pattern.ROWS => obj.ToObject<RowsColsLayoutGrid>(serializer),
                Pattern.GRID => obj.ToObject<GridLayoutGrid>(serializer),
                _ => throw new NotSupportedException()
            };
        #endregion
    }

    public class ExportSettingsArrayConverter : ArrayConverter<ExportSettings, Format>
    {
        #region Methods
        protected override ExportSettings ToObject(JObject obj, JsonSerializer serializer) =>
            GetValue(obj, "format") switch
            {
                Format.JPG => obj.ToObject<ExportSettingsImage>(serializer),
                Format.PNG => obj.ToObject<ExportSettingsImage>(serializer),
                Format.SVG => obj.ToObject<ExportSettingsSVG>(serializer),
                Format.PDF => obj.ToObject<ExportSettingsPDF>(serializer),
                _ => throw new NotSupportedException()
            };
        #endregion
    }

    public class TransitionConverter : JsonConverter
    {
        #region Methods
        public override bool CanConvert(Type objectType) => objectType == typeof(Transition);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            return (TransitionType)Enum.Parse(typeof(TransitionType), obj["type"]!.Value<string>()) switch
            {
                TransitionType.DISSOLVE => obj.ToObject<SimpleTransition>(serializer),
                TransitionType.SMART_ANIMATE => obj.ToObject<SimpleTransition>(serializer),
                TransitionType.MOVE_IN => obj.ToObject<DirectionalTransition>(serializer),
                TransitionType.MOVE_OUT => obj.ToObject<DirectionalTransition>(serializer),
                TransitionType.PUSH => obj.ToObject<DirectionalTransition>(serializer),
                TransitionType.SLIDE_IN => obj.ToObject<DirectionalTransition>(serializer),
                TransitionType.SLIDE_OUT => obj.ToObject<DirectionalTransition>(serializer),
                _ => throw new NotSupportedException()
            };
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
        #endregion
    }

    public class BaseNodeArrayConverter : ArrayConverter<BaseNode, NodeType>
    {
        #region Methods
        protected override BaseNode ToObject(JObject obj, JsonSerializer serializer) =>
            GetValue(obj) switch
            {
                NodeType.DOCUMENT => obj.ToObject<DocumentNode>(serializer),
                NodeType.CANVAS => obj.ToObject<CanvasNode>(serializer),
                _ => throw new NotSupportedException()
            };
        #endregion
    }

    public class SceneNodeArrayConverter : ArrayConverter<SceneNode, NodeType>
    {
        #region Methods
        protected override SceneNode ToObject(JObject obj, JsonSerializer serializer) =>
            GetValue(obj) switch
            {
                NodeType.SLICE => obj.ToObject<SliceNode>(serializer),
                NodeType.FRAME => obj.ToObject<FrameNode>(serializer),
                NodeType.GROUP => obj.ToObject<GroupNode>(serializer),
                NodeType.COMPONENT_SET => obj.ToObject<ComponentSetNode>(serializer),
                NodeType.COMPONENT => obj.ToObject<ComponentNode>(serializer),
                NodeType.INSTANCE => obj.ToObject<InstanceNode>(serializer),
                NodeType.BOOLEAN_OPERATION => obj.ToObject<BooleanOperationNode>(serializer),
                NodeType.VECTOR => obj.ToObject<VectorNode>(serializer),
                NodeType.STAR => obj.ToObject<StarNode>(serializer),
                NodeType.LINE => obj.ToObject<LineNode>(serializer),
                NodeType.ELLIPSE => obj.ToObject<EllipseNode>(serializer),
                NodeType.REGULAR_POLYGON => obj.ToObject<RegularPolygonNode>(serializer),
                NodeType.RECTANGLE => obj.ToObject<RectangleNode>(serializer),
                NodeType.TEXT => obj.ToObject<TextNode>(serializer),
                NodeType.SECTION => obj.ToObject<SectionNode>(serializer),
                _ => throw new NotSupportedException()
            };
        #endregion
    }

    public class FigmaGeneration { }
}