namespace DataServer
{
    public enum CallOutOrientation
    {
        Top,
        Right,
        RightSideMenu
    }

    public enum TapMode
    {
        None,
        CallOut,       
        CallOutPopup,
        Popup,
        OpenMedia,
        Zoom,
        CustomEvent,
        CallOutEdit
    }

    public enum TitleModes
    {
        /// <summary>
        /// When using None, it means that the PoI will never have a title. 
        /// This is more efficient, as we don't need to create a text symbol graphic for it.
        /// </summary>
        None,
        /// <summary>
        /// Create a graphic for the title and display the label refered to by Style.NameLabel.
        /// </summary>
        Bottom
    }

    public enum TimelineBehaviours
    {
        None,
        BeforeFocus,
        InRange        
    }

    public enum AddModes
    {
        Silent,
        EditFirst,
        OpenAfter
    }
    
   
}