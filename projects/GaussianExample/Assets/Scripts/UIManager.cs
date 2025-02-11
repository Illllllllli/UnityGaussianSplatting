using System.Collections;
using System.Collections.Generic;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Screen = UnityEngine.Device.Screen;

public class UIManager : MonoBehaviour
{
    public GameObject gaussianSplats;
    public Button viewButton;
    public Button selectButton;
    public Button editButton;
    public Camera mainCamera;
    private GaussianSplatRenderer gsRenderer;

    

    
    // Start is called before the first frame update
    void Start()
    {
        selectButton.onClick.AddListener(SwitchSelectMode);
        viewButton.onClick.AddListener(SwitchViewMode);
        editButton.onClick.AddListener(SwitchEditMode);
        //绑定gs对象
        gsRenderer = gaussianSplats.GetComponent<GaussianSplatRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void SwitchSelectMode()
    {
        // gsRenderer.EditUpdateSelection(new Vector2(216.00f,643.00f),new Vector2(688.00f,167.00f),mainCamera,false);
        Status._mode = Mode.Select;
        Debug.Log("select");
    }
    void SwitchViewMode()
    {
        Status._mode = Mode.View;
        gsRenderer.EditDeselectAll();
        Debug.Log("view");
    }

    void SwitchEditMode()
    {
        Status._mode = Mode.Edit;
        gsRenderer.EditDeselectAll();
        Debug.Log("edit");
    }
}

//选择工具函数类
internal static class SelectTool
{
    public static Vector2 MouseStartDragPos=Vector2.zero;
    public static Rect FromToRect(Vector2 from, Vector2 to)
    {
        
        if (from.x > to.x)
            (from.x, to.x) = (to.x, from.x);
        if (from.y > to.y)
            (from.y, to.y) = (to.y, from.y);
        return new Rect(from.x, from.y, to.x - from.x, to.y - from.y);
    }
}