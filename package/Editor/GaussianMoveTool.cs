// SPDX-License-Identifier: MIT

using GaussianSplatting.Runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    [EditorTool("Gaussian Move Tool", typeof(GaussianSplatRenderer), typeof(GaussianToolContext))]
    class GaussianMoveTool : GaussianTool
    {
        public override void OnToolGUI(EditorWindow window)
        {
            var gs = GetRenderer();
            if (!gs || !CanBeEdited() || !HasSelection())
                return;
            var tr = gs.transform;

            // 监听GUI的修改操作
            EditorGUI.BeginChangeCheck();
            // 获取选中元素在本地坐标系中的中心位置
            var selCenterLocal = GetSelectionCenterLocal();
            // 将本地坐标转换为世界坐标
            var selCenterWorld = tr.TransformPoint(selCenterLocal);
            var newPosWorld = Handles.DoPositionHandle(selCenterWorld, Tools.handleRotation);
            // 判断位置是否被修改
            if (EditorGUI.EndChangeCheck())
            {
                // 将新的世界坐标转换回本地坐标系
                var newPosLocal = tr.InverseTransformPoint(newPosWorld);
                var wasModified = gs.editModified;
                // 根据偏移量移动选中项
                gs.EditTranslateSelection(newPosLocal - selCenterLocal);
                // 如果首次修改，调用 RepaintAll() 刷新所有相关视图
                if (!wasModified)
                    GaussianSplatRendererEditor.RepaintAll();
                Event.current.Use();
            }
        }
    }
}
