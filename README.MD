> [!WARNING]
> **Experimental Release**: This plugin is currently in an experimental phase and is provided "as is" without warranty of any kind. It was originally developed for internal use and may contain issues or limitations. Use it at your own risk. Feedback and contributions are welcome but please keep in mind the experimental nature of this tool.

# Overview
FigmaToUnity is a specialized Unity tool that streamlines the UI development process by enabling the direct import of Figma page documents into Unity. The tool automatically converts Figma designs into UI Toolkit assets, allowing for quick and accurate integration of UI interfaces into your Unity games.

# Features
| Name                            | Description                                                                                                                                         |
|---------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| **Figma Import to UXML/USS**    | The tool imports and parses Figma page documents, transforming them into UXML and USS assets within Unity.                                          |
| **Element Manipulation**        | Enables the manipulation of UI elements and the application of custom logic via Unity scripts, providing extensive control over the user interface. |
| **High Figma Compatibility**    | Approximately 85% of Figma's design tools are supported, ensuring high compatibility for a majority of UI design needs.                             |
| **Sync Changes**                | Changes made to UI elements in Figma can be readily fetched and updated in Unity, maintaining the integrity of the game's UI.                       |
| **[Limitations](#limitations)** | Refer to the section below.                                                                                                                         |

# Installing
1. Open `Window > Package Manager`
2. Click the + button in the top-left corner
3. Choose `Add package from git URL...`
4. Enter https://github.com/TrackMan/Unity.Package.FigmaForUnity.git

# Dependencies
To integrate these dependencies, you must either manually include them in your project's manifest file or ensure they are automatically resolved through Unity's Package Manager registry.
- [Newtonsoft.Json 13.0.1](https://docs.unity3d.com/Packages/com.unity.vectorgraphics@2.0/manual/index.html)
- [Unity Vector Graphics 2.0.0-preview.21](https://docs.unity3d.com/Packages/com.unity.vectorgraphics@2.0/manual/index.html)
- [Async Await Util 1.0.6](https://github.com/TrackMan/Unity.Package.AsyncAwaitUtil)
- [Common Utils 3.7.0](https://github.com/TrackMan/Unity.Package.CommonUtils)

# Quick Start (WIP)
Here we want to explain howto use our example folder ~Samples in the repository to quick start.

# Usage (WIP)
We need to write a quick overview of the overall system and how we use it daily.
Then we can explain the basic concepts of the system and go into more details in further sections.

## Basic overview
## Figma Inspector
## Figma class
## UxmlAttribute
## QueryAttribute
## Elements and Attributes
## Searching elements
## Cloning elements
## Using Attributes, full list of them
## Working with Element class (also events OnInitialize, OnRebuild)
## Interfaces ISubElement, IRootElement
## Working with styles (replacement and changing styles)

| Property                           | Description                                                                                                                                                                                            |
|------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Panel Settings**                 | A crucial asset enabling Unity to render UXML-based user interfaces within the Game View.                                                                                                              |
| **Source Asset**                   | The UXML asset responsible for outlining the structural framework of the user interface.                                                                                                               |
| **Sort Order**                     | Specifies the rendering sequence of UXML assets when multiple Panel Settings instances exist within a project.                                                                                         |
| **Title**                          | Designates the title from the Figma document URL for identification purposes.                                                                                                                          |
| **Asset**                          | Represents the UXML asset correlating with the corresponding asset within the UI document script. (Refer to the section above for further details)                                                     |
| **Update Buttons**                 | Facilitates UI updates, offering options to include or exclude the downloading of all associated images.                                                                                               |
| **De-root and Re-order Hierarchy** | Adjusts the organization of all frames based on each element's `RootOrder` property, optimizing the UI hierarchy.                                                                                      |
| **Filter by Path**                 | When activated, this feature limits the download to only those UI frames that have associated scripts attached to the prefab, otherwise, all UI elements within the Figma document will be downloaded. |
| **Additional Fonts Directories**   | Provides the capability to specify paths to any fonts that are incorporated within the UI, ensuring seamless visual consistency.                                                                       |

# Limitations
| Feature                      | Description                                                                                                                             |
|------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| **Unique Frame Names**       | Each frame must have a unique name to ensure proper functionality.                                                                      |
| **Vector Constraints**       | Vectors should be visible, should not contain Image Fills, and, where possible, should be grouped into Unions.                          |
| **Fill Limitations**         | Each UI element can contain only a single Fill attribute.                                                                               |
| **Auto-Layout Restrictions** | Stroke borders are not supported within Auto-Layout configurations.                                                                     |
| **Alignment Constraints**    | Horizontal and vertical centering cannot be mixed within the same parent element; doing so will default the alignment to center-center. |
| **Circle Representation**    | For optimal visual rendering, circles should be implemented using rectangles rather than ellipses.                                      |

# Not implemented
- Flex-gap (Unity not supported)
- Blur (Unity not supported)
- Letter spacing (Unity not supported)
- Line height (Unity not supported)
- IndividualStrokeWeights (Unity not supported)
- ComponentPropertyDefinitions
- Various CodeGenerators
- Search by name/class/path
- Dragging items for scroll-view
- Generation of the various states of Elements with states

# --------- DRAFT TEXT TO RE REUSED ABOVE ---------

## Figma Prefab
![image](https://github.com/anton-trackman/test/blob/main/FigmaPrefab.png)

### Personal Access Token (this after we explained about Figma prefab)
To utilize FigmaToUnity, a Figma Personal Access Token is needed for API calls.
> [!WARNING]
> The token is stored in raw format.
1. Visit the [Figma API Authentication Page](https://www.figma.com/developers/api?fuid=797042793200923967#authentication).
2. Click `+ Get personal access token` to generate the token.
3. Copy the generated token.
4. Locate the Figma script in Unity's Inspector.
5. Paste the token into the designated field.

### Frame Script Anatomy
```csharp
[Uxml("SamplePage/SampleFrame", UxmlDownloadImages.Everything, UxmlElementTypeIdentification.ByElementType)]
public class SampleFrame : Element
{
    #region Fields
    [Query("SampleButton", Clicked = nameof(Test))] Button sampleButton;
    #endregion

    #region Methods
    void Test() => Debug.Log("Test");
    #endregion
}
```
As you can see the simple frame script consist of: Uxml attribute, Frame class which inherits from Element, fields with Query attributes

### Attributes
**Uxml attribute**
```csharp
public UxmlAttribute(string root = default, UxmlDownloadImages imageFiltering = UxmlDownloadImages.Everything, UxmlElementTypeIdentification typeIdentification = UxmlElementTypeIdentification.ByName, params string[] preserve)
```
* **root.** Represents the path to the frame. Example: "SamplePage/SampleFrame"
* **imageFiltering.** Which type images should be downloaded. Example: UxmlDownloadImages.Everything
* **typeIdentification.** By which way element types should be defined. Example: UxmlElementTypeIdentification.ByElementType
* **preserve**. Root folders of additional content frames. Example: "SamplePage/Content"
  
**Query attribute**
```csharp
public QueryAttribute(string path, Clicked = nameof(Test), StartRoot = true, StopRoot = true, Nullable = true, Template = "SampleTemplate", UseTrickleDown = TrickleDown.TrickleDown)
```
* **path.** Represents the path to the element
* **Clicked.** Defines the method which should be executed

### Element Types
* VisualElement
* Button
* ScrollView

### Custom Elements
As addition to standard elements, you can create your own custom elements
```csharp
public class CustomButton : Button
{
    public new class UxmlFactory : UxmlFactory<CustomButton> { }

    #region Fields
    [Query("CustomButtonLabel")] Label label;
    #endregion
}
```