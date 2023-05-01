using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Trackman;
using UnityEngine;

namespace Figma
{
    namespace global
    {
        public class JsonUtility : JsonUtilityShared<FigmaGeneration>
        {
            #region Constructors
#if UNITY_EDITOR
            [UnityEditor.InitializeOnLoadMethod]
#endif
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            public static void Initialize()
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.Converters.Add(new EffectArrayConverter());
                settings.Converters.Add(new PaintArrayConverter());
                settings.Converters.Add(new LayoutGridArrayConverter());
                settings.Converters.Add(new ExportSettingsArrayConverter());
                settings.Converters.Add(new TransitionConverter());
                settings.Converters.Add(new BaseNodeArrayConverter());
                settings.Converters.Add(new SceneNodeArrayConverter());
                settings.MissingMemberHandling = MissingMemberHandling.Error;
                serializer = JsonSerializer.Create(settings);
            }
            #endregion
        }

        public abstract class ArrayConverter<T, U> : JsonConverter
        {
            #region Methods
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(T[]);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JArray array = JArray.Load(reader);
                T[] result = new T[array.Count];
                for (int i = 0; i < array.Count; ++i) result[i] = ToObject((JObject)array[i], serializer);
                return result;
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                T[] array = (T[])value;
                writer.WriteStartArray();
                foreach (T node in array) serializer.Serialize(writer, node);
                writer.WriteEndArray();
            }
            protected U GetValue(JObject obj, string name = "type")
            {
                return (U)Enum.Parse(typeof(U), obj[name].Value<string>());
            }
            protected abstract T ToObject(JObject obj, JsonSerializer serializer);
            #endregion
        }

        public class EffectArrayConverter : ArrayConverter<Effect, EffectType>
        {
            #region Methods
            protected override Effect ToObject(JObject obj, JsonSerializer serializer)
            {
                switch (GetValue(obj))
                {
                    case EffectType.INNER_SHADOW:
                    case EffectType.DROP_SHADOW:
                        return obj.ToObject<ShadowEffect>(serializer);

                    case EffectType.LAYER_BLUR:
                    case EffectType.BACKGROUND_BLUR:
                        return obj.ToObject<BlurEffect>(serializer);

                    default:
                        throw new NotSupportedException();
                }
            }
            #endregion
        }

        public class PaintArrayConverter : ArrayConverter<Paint, PaintType>
        {
            #region Methods
            protected override Paint ToObject(JObject obj, JsonSerializer serializer)
            {
                switch (GetValue(obj))
                {
                    case PaintType.SOLID:
                        return obj.ToObject<SolidPaint>(serializer);

                    case PaintType.GRADIENT_LINEAR:
                    case PaintType.GRADIENT_RADIAL:
                    case PaintType.GRADIENT_ANGULAR:
                    case PaintType.GRADIENT_DIAMOND:
                        return obj.ToObject<GradientPaint>(serializer);

                    case PaintType.IMAGE:
                    case PaintType.EMOJI:
                        return obj.ToObject<ImagePaint>(serializer);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            #endregion
        }

        public class LayoutGridArrayConverter : ArrayConverter<LayoutGrid, Pattern>
        {
            #region Methods
            protected override LayoutGrid ToObject(JObject obj, JsonSerializer serializer)
            {
                switch (GetValue(obj, "pattern"))
                {
                    case Pattern.COLUMNS:
                    case Pattern.ROWS:
                        return obj.ToObject<RowsColsLayoutGrid>(serializer);

                    case Pattern.GRID:
                        return obj.ToObject<GridLayoutGrid>(serializer);

                    default:
                        throw new NotSupportedException();
                }
            }
            #endregion
        }

        public class ExportSettingsArrayConverter : ArrayConverter<ExportSettings, Format>
        {
            #region Methods
            protected override ExportSettings ToObject(JObject obj, JsonSerializer serializer)
            {
                switch (GetValue(obj, "format"))
                {
                    case Format.JPG:
                    case Format.PNG:
                        return obj.ToObject<ExportSettingsImage>(serializer);

                    case Format.SVG:
                        return obj.ToObject<ExportSettingsSVG>(serializer);

                    case Format.PDF:
                        return obj.ToObject<ExportSettingsPDF>(serializer);

                    default:
                        throw new NotSupportedException();
                }
            }
            #endregion
        }

        public class TransitionConverter : JsonConverter
        {
            #region Methods
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Transition);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);
                switch ((TransitionType)Enum.Parse(typeof(TransitionType), obj["type"].Value<string>()))
                {
                    case TransitionType.DISSOLVE:
                    case TransitionType.SMART_ANIMATE:
                        return obj.ToObject<SimpleTransition>(serializer);

                    case TransitionType.MOVE_IN:
                    case TransitionType.MOVE_OUT:
                    case TransitionType.PUSH:
                    case TransitionType.SLIDE_IN:
                    case TransitionType.SLIDE_OUT:
                        return obj.ToObject<DirectionalTransition>(serializer);

                    default:
                        throw new NotSupportedException();
                }
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
            #endregion
        }

        public class BaseNodeArrayConverter : ArrayConverter<BaseNode, NodeType>
        {
            #region Methods
            protected override BaseNode ToObject(JObject obj, JsonSerializer serializer)
            {
                switch (GetValue(obj))
                {
                    case NodeType.DOCUMENT:
                        return obj.ToObject<DocumentNode>(serializer);

                    case NodeType.CANVAS:
                        return obj.ToObject<CanvasNode>(serializer);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            #endregion
        }

        public class SceneNodeArrayConverter : ArrayConverter<SceneNode, NodeType>
        {
            #region Methods
            protected override SceneNode ToObject(JObject obj, JsonSerializer serializer)
            {
                switch (GetValue(obj))
                {
                    case NodeType.SLICE:
                        return obj.ToObject<SliceNode>(serializer);

                    case NodeType.FRAME:
                        return obj.ToObject<FrameNode>(serializer);

                    case NodeType.GROUP:
                        return obj.ToObject<GroupNode>(serializer);

                    case NodeType.COMPONENT_SET:
                        return obj.ToObject<ComponentSetNode>(serializer);

                    case NodeType.COMPONENT:
                        return obj.ToObject<ComponentNode>(serializer);

                    case NodeType.INSTANCE:
                        return obj.ToObject<InstanceNode>(serializer);

                    case NodeType.BOOLEAN_OPERATION:
                        return obj.ToObject<BooleanOperationNode>(serializer);

                    case NodeType.VECTOR:
                        return obj.ToObject<VectorNode>(serializer);

                    case NodeType.STAR:
                        return obj.ToObject<StarNode>(serializer);

                    case NodeType.LINE:
                        return obj.ToObject<LineNode>(serializer);

                    case NodeType.ELLIPSE:
                        return obj.ToObject<EllipseNode>(serializer);

                    case NodeType.REGULAR_POLYGON:
                        return obj.ToObject<RegularPolygonNode>(serializer);

                    case NodeType.RECTANGLE:
                        return obj.ToObject<RectangleNode>(serializer);

                    case NodeType.TEXT:
                        return obj.ToObject<TextNode>(serializer);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            #endregion
        }

        public class FigmaGeneration
        {
        }
    }
}