> [!WARNING]
> **Experimental Release**: This plugin is currently in an experimental phase and is provided "as is" without warranty of any kind. It was originally developed for internal use and may contain issues or limitations. Use it at your own risk. Feedback and contributions are welcome but please keep in mind the experimental nature of this tool.

[output.webm](https://github.com/TrackMan/Unity.Package.FigmaForUnity/assets/22183046/59423710-61af-44b0-b233-47216881c051)

# Overview
FigmaToUnity is a specialized Unity tool that streamlines the UI development process by enabling the direct import of Figma page documents into Unity. The tool automatically converts Figma designs into UI Toolkit assets, allowing for quick and accurate integration of UI interfaces into your Unity games.

# Features

| Name                            | Description                                                                                                                                         |
|---------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| **Figma Import to UXML/USS**    | The tool imports and parses Figma page documents, transforming them into UXML and USS assets within Unity.                                          |
| **Element Manipulation**        | Enables the manipulation of UI elements and the application of custom logic via Unity scripts, providing extensive control over the user interface. |
| **Sync Changes**                | Changes made to UI elements in Figma can be readily fetched and updated in Unity, maintaining the integrity of the game's UI.                       |
| **[Limitations](#limitations)** | Refer to the section below.                                                                                                                         |

# Installing
1. Open `Window > Package Manager`
2. Click the `+` button in the top-left corner
3. Choose `Add package from git URL...`
4. Enter https://github.com/TrackMan/Unity.Package.AsyncAwaitUtil.git
5. Click the `+` button in the top-left corner
6. Choose `Add package from git URL...`
7. Enter https://github.com/TrackMan/Unity.Package.CommonUtils.git
8. Click the `+` button in the top-left corner
9. Choose `Add package from git URL...`
10. Enter https://github.com/TrackMan/Unity.Package.FigmaForUnity.git

## Dependencies
To integrate these dependencies, you must either manually include them in your project's manifest file or ensure they are automatically resolved through Unity's Package Manager registry.

- [Async Await Util 1.0.6](https://github.com/TrackMan/Unity.Package.AsyncAwaitUtil)
- [Common Utils 3.8.0](https://github.com/TrackMan/Unity.Package.CommonUtils)

## Personal Access Token
To start using [Figma Inspector](#Figma-Inspector), a Figma Personal Access Token is needed for API calls.
> [!WARNING]
> The token is stored in raw format.

1. Visit the [Figma API Authentication Page](https://www.figma.com/developers/api?fuid=797042793200923967#authentication).
2. Click + Get personal access token to generate the token.
3. Copy the generated token.
4. Locate the Figma script in [Unity's Inspector](#Figma-Inspector).
5. Paste the token into the designated field.

# Quick Start
- Finish [Installing](#installing)
- Open `~Samples` folder
- Open `Test.unity`
- Go to `Figma` GameObject
- In the `Title`, enter the title of your Figma document (ie dfeQabSU71CHXVqweameSF) from Figma website ([some templates](https://www.figma.com/community/files))
- Go to `Test.cs` and edit the UXML attribute to points to your Page/Element path
- Configure [Personal Access Token](#Personal-Access-Token) in [Unity's Inspector](#Figma-Inspector)
- Click `Update UI & Images`
- Save the `VisualAssetTree` somewhere
- Start `playmode` and enjoy!

# Usage
Working with this plugin is done through using Figma component inspector FigmaInspector. 
In addition, components derived from the Element class should be created and added to the same GameObject hierarchy.
These components serve a dual purpose:

1. During the import phase, they assist in filtering and configuring various aspects of the document, such as frame selection and image handling.
2. During runtime, they provide the functionality to manipulate UXML and USS data structures.

## Figma Inspector
![image](https://user-images.githubusercontent.com/22183046/270277550-bd127c6a-1e0f-4494-8b2b-cc9e87ed0448.png)

| Property                           | Description                                                                                                                                                                                            |
|------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Panel Settings**                 | A crucial asset enabling Unity to render UXML-based user interfaces within the Game View.                                                                                                              |
| **Source Asset**                   | The UXML asset responsible for outlining the structural framework of the user interface.                                                                                                               |
| **Sort Order**                     | Specifies the rendering sequence of UXML assets when multiple Panel Settings instances exist within a project.                                                                                         |
| **Title**                          | Designates the title from the Figma document URL for identification purposes.                                                                                                                          |
| **Asset**                          | Represents the UXML asset correlating with the corresponding asset within the UI document script. (Refer to the section above for further details)                                                     |
| **Update Buttons**                 | Facilitates UI updates, offering options to include or exclude the downloading of all associated images.                                                                                               |
| **De-root and Re-order Hierarchy** | Adjusts the organization of all frames based on each element's RootOrder property, optimizing the UI hierarchy.                                                                                        |
| **Filter by Path**                 | When activated, this feature limits the download to only those UI frames that have associated scripts attached to the prefab, otherwise, all UI elements within the Figma document will be downloaded. |
| **Additional Fonts Directories**   | Provides the capability to specify paths to any fonts that are incorporated within the UI, ensuring seamless visual consistency.                                                                       |

## Folders Layout
This is how the final setup should look:
```
<UI folder>
  ├── UI.uxml
  ├── UI.uss
  ├── UI.asset
  ├── remaps_UI.json
  ├── Elements
  │   └── CustomElement.uxml
  ├── Images
  │   ├── AmazingImage.svg
  │   └── PerfectImage.png
  └── Fonts
      └── Inter-Regular.ttf
```
Please note that you need to place your **custom fonts** in a **'Fonts' folder**. If you try to import a Figma document that includes a font which does not exist in the 'Fonts' folder, you will see a message in the console indicating which font is missing. You can also add an additional fonts directory using the appropriate property in the Figma object.

## Figma class
During the update process the Figma class retrieves data based on the Uxml and Query attributes of the Element scripts. It then utilizes this data while producing UXML asset.
> [!WARNING]
> Element scripts should be attached to the same game object to which the Figma script is also attached.

## Element class (OnInitialize, OnRebuild, Custom Elements)
The Uxml and Query attributes define the structure of the Uxml asset and, consequently, the appearance of your UI.
The type of the field following the Query attribute defines the UI element itself and, consequently, its behavior (VisualElement, Button, Label, etc.). Field types are written into UXML. Therefore, if you change the field type (for example, from VisualElement to Button), you will need to perform a Figma Update to regenerate the UXML.
Each element can override OnInitialize and OnRebuild methods which can be used to do any initial setup operations.

``` csharp
[Uxml("TestPage/TestFrame", UxmlDownloadImages.Everything, UxmlElementTypeIdentification.ByElementType)]
[AddComponentMenu("Figma/Samples/Test")]
public class Test : Element
{
	const int minCircles = 1;
	const int maxCircles = 7;

	#region Fields
	[Query("Header")] Label header;

	[Query("CloneButton", Clicked = nameof(Clone))] Button cloneButton;
	[Query("RemoveButton", Clicked = nameof(Remove))] Button removeButton;
	[Query("CloneContainer", StartRoot = true)] VisualElement cloneContainer;
	[Query("CloneCircle", EndRoot = true)] PerfectCircle cloneCircle;

	[Query("SyncButton", Clicked = nameof(Sync))] Button syncButton;
	[Query("SyncContainer")] VisualElement syncContainer;
	[Query("SyncContainer/SyncCircle")] PerfectCircle syncCircle;

	[Query("FunctionDescription", Hide = true)] Label functionDescription;
	#endregion

	#region Methods
	protected override void OnInitialize() => cloneContainer.style.flexWrap = Wrap.NoWrap;
	protected override void OnRebuild() => header.text = "Welcome to Figma Test Frame!";

	void Clone()
	{
		if (cloneContainer.childCount == maxCircles) return;

		cloneCircle.Clone(cloneContainer);
	}
	void Remove()
	{
		if (cloneContainer.childCount == minCircles) return;

		cloneContainer.Remove(cloneContainer.Children().First());
	}
	void Sync()
	{
		void RandomColor(int index) => syncContainer.Children().ElementAt(index).style.backgroundColor = Random.ColorHSV();

		syncCircle.Sync(syncContainer, RandomColor, Enumerable.Range(0, Random.Range(1, maxCircles + 1)));
		syncCircle.Hide();

		functionDescription.Show();
	}
	#endregion
}
```
### Custom element example
``` csharp
public class PerfectCircle : SyncButtonSimple<int>
{
	public new class UxmlFactory : UxmlFactory<PerfectCircle> { }

	#region Methods
	public override bool IsVisible(int index, int data) => true;
	#endregion
}
```

## UxmlAttribute
This attribute specifies the Element (a frame, and it's children) that we want to import from Figma document.

| Attribute              | Description                                                                                                                            |
|------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| **Root**               | Defines the root path within the Figma document where this frame originates, inclusive of the canvas path.                             |
| **ImageFiltering**     | Specifies the strategy for downloading images from the Figma document.                                                                 |
| **TypeIdentification** | Indicates the method for identifying the types of elements, whether based on their name or their classification under Element classes. |
| **Preserve**           | Lists any additional paths that should be maintained as-is in the imported document.                                                   |

## QueryAttribute
This attribute specifies the sub element (inside of the Element) parameters (like path to element, or what should happen when you click button).

| Event Name                      | Description                                                              |
|---------------------------------|--------------------------------------------------------------------------|
| **Path**                        | The path used in the UI query.                                           |
| **ClassName**                   | The class name of the element.                                           |
| **ImageFiltering**              | Enum specifying how images are downloaded or filtered.                   |
| **ReplaceNodePath**             | Path to the node that will be replaced.                                  |
| **ReplaceNodeEvent**            | Event that triggers the node replacement.                                |
| **ReplaceElementPath**          | Path to the element that will be replaced.                               |
| **RebuildElementEvent**         | Event that triggers the element to be rebuilt.                           |
| **StartRoot**                   | Specifies that element path will be new root for the following elements. |
| **EndRoot**                     | Specifies the end of StartRoot.                                          |
| **Nullable**                    | Specifies if the element can be null.                                    |
| **Clicked**                     | Name of the method to be invoked when the element is clicked.            |
| **Template**                    | Template to be used for the element (creates a separate uxml file).      |
| **UseTrickleDown**              | Specifies if events should trickle down through the element hierarchy.   |
| **ChangeEvent**                 | Event triggered when an element's state changes.                         |
| **MouseCaptureOutEvent**        | Event triggered when mouse capture is lost.                              |
| **ValidateCommandEvent**        | Event triggered to validate a command.                                   |
| **ExecuteCommandEvent**         | Event triggered to execute a command.                                    |
| **DragExitedEvent**             | Event triggered when a drag operation exits the element.                 |
| **DragUpdatedEvent**            | Event triggered when a drag operation is updated.                        |
| **DragPerformEvent**            | Event triggered when a drag operation is performed.                      |
| **DragEnterEvent**              | Event triggered when a drag operation enters the element.                |
| **DragLeaveEvent**              | Event triggered when a drag operation leaves the element.                |
| **FocusOutEvent**               | Event triggered when the element loses focus.                            |
| **BlurEvent**                   | Event triggered when the element is blurred.                             |
| **FocusInEvent**                | Event triggered when the element gains focus.                            |
| **FocusEvent**                  | Event triggered when the element is focused or loses focus.              |
| **InputEvent**                  | Event triggered when the element receives input.                         |
| **KeyDownEvent**                | Event triggered when a key is pressed down.                              |
| **KeyUpEvent**                  | Event triggered when a key is released.                                  |
| **GeometryChangedEvent**        | Event triggered when the element's geometry changes.                     |
| **PointerDownEvent**            | Event triggered when a pointer is pressed down.                          |
| **PointerUpEvent**              | Event triggered when a pointer is released.                              |
| **PointerMoveEvent**            | Event triggered when a pointer is moved.                                 |
| **MouseDownEvent**              | Event triggered when a mouse button is pressed.                          |
| **MouseUpEvent**                | Event triggered when a mouse button is released.                         |
| **MouseMoveEvent**              | Event triggered when the mouse is moved.                                 |
| **ContextClickEvent**           | Event triggered on a context click (right-click).                        |
| **WheelEvent**                  | Event triggered when the mouse wheel is moved.                           |
| **MouseEnterEvent**             | Event triggered when the mouse enters the element.                       |
| **MouseLeaveEvent**             | Event triggered when the mouse leaves the element.                       |
| **MouseEnterWindowEvent**       | Event triggered when the mouse enters the window containing the element. |
| **MouseLeaveWindowEvent**       | Event triggered when the mouse leaves the window containing the element. |
| **MouseOverEvent**              | Event triggered when the mouse is over the element.                      |
| **MouseOutEvent**               | Event triggered when the mouse is out of the element.                    |
| **ContextualMenuPopulateEvent** | Event triggered to populate the contextual menu.                         |
| **AttachToPanelEvent**          | Event triggered when the element is attached to a panel.                 |
| **DetachFromPanelEvent**        | Event triggered when the element is detached from a panel.               |
| **TooltipEvent**                | Event triggered to display a tooltip.                                    |
| **IMGUIEvent**                  | Event triggered for IMGUI rendering.                                     |

## ISubElement
This interface serves as an identifier, signifying that within the IRootElement hierarchy, an element exists which can function as a component based on the VisualElement class.

## IRootElement
This interface acts as a marker, indicating that an element within the IRootElement hierarchy is capable of functioning as a component derived from the VisualElement class.

## Visual Element Style
The imported USS file contains all the classes used by Visual Elements. USS is inspired by CSS and has a similar syntax. To manipulate properties in your code, you should use the techniques described below.
### Changing Element Appearance
To change the appearance of a VisualElement, you should manipulate its style properties. For example, to set the top margin of an element to 3:
```csharp
element.style.marginTop = 3;
```
### Reading Element Style Data
If you need to read style data from a VisualElement, you should use its resolved style properties. For example, to retrieve the value of the top margin:
```csharp
float margin = element.resolvedStyle.marginTop;
```
> You can find the list of all supported properties in the official Unity documentation.

## Visual Element Extensions
When working with VisualElements, there are various visual element extensions that allow you to find/clone/replace/etc elements:
```csharp
VisualElement rectangle = element.Find<VisualElement>("Rectangle");
```
```csharp
VisualElement elementClone = options.Clone();
```
```csharp
element.Replace(newElement);
```

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
- Documentation (work in progress)
- Flex-gap (Unity not supported)
- Blur (Unity not supported)
- Letter spacing (Unity not supported)
- Line height (Unity not supported)
- IndividualStrokeWeights (Unity not supported)
- ComponentPropertyDefinitions
- Various CodeGenerators
- Search by name/class/path
- Dragging items for scroll-view
- Generation of the various states of Elements with states- Generation of the various states of Elements with states- Generation of the various states of Elements with states