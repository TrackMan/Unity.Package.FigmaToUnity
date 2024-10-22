using System;

namespace Figma
{
    using Attributes;

    record RootMetadata(bool filter, UxmlAttribute uxml, UxmlDownloadImages downloadImages);

    // ReSharper disable once NotAccessedPositionalProperty.Global
    record QueryMetadata(Type fieldType, QueryAttribute query);

    record BaseNodeMetadata(RootMetadata root, QueryMetadata query);
}