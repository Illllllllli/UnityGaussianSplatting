using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Status : MonoBehaviour
{
    //查看器模式（浏览/选择/编辑）
    
    public static Mode _mode;

}
public enum Mode
    {
        View,Select,Edit
    }