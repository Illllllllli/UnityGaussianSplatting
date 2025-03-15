using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GaussianProfile
{
    public string ScriptPath;
    public string ConfigPath;
    // 训练模式（默认开启）
    private const bool Train = true;
    // GPU的编号
    private const int Gpuid = 0;
    // colmap文件夹路径以及gs文件路径
    public string DataSourcePath;
    public string GsSourcePath;
    // 最大训练步数
    public uint MaxTrainSteps;
    // 
    // 学习率相关
    public float GsLrScaler;
    public float GsFinalLrScaler;
    public float GsColorLrScaler;
    public float GsOpacityLrScaler;
    public float GsScalingLrScaler;
    public float GsRotationLrScaler;
    // 是否覆盖缓存
    private const bool CacheOverwrite = false;
    // 是否开启wandb日志（默认关闭）
    private const bool EnableWandb = false;

    /// <summary>
    /// 预处理指令中带引号的字符串
    /// </summary>
    /// <param name="s">字符串</param>
    /// <returns>处理后的字符串</returns>
    protected static string PrepareString(string s)
    {
        return "'" + s.Replace("'", "\\'").Replace("\"","\\\"") + "'";
    }

    protected bool GetGeneralCommand(out string command)
    {

        if (ScriptPath.Length<=0||ConfigPath.Length<=0||DataSourcePath.Length<=0||GsSourcePath.Length<=0)
        {
            command = "Internal file path not completed";
            return false;
        }
        if (MaxTrainSteps <= 0)
        {
            command = "Please input a valid Max Train Steps (>0)";
            return false;
        }
        
        command =
                $"{ScriptPath} --config {ConfigPath} --train --gpu {Gpuid} data.source={DataSourcePath} system.gs_source={GsSourcePath} " +
                $" system.gs_lr_scaler={GsLrScaler} system.gs_final_lr_scaler={GsFinalLrScaler} system.color_lr_scaler={GsColorLrScaler} system.opacity_lr_scaler={GsOpacityLrScaler} system.scaling_lr_scaler={GsScalingLrScaler} system.rotation_lr_scaler={GsRotationLrScaler} "+
                $" system.cache_overwrite={CacheOverwrite} trainer.max_steps={MaxTrainSteps} system.loggers.wandb.enable={EnableWandb} ";
        return true;
            
    }
    /// <summary>
    /// 获取该配置类生成的指令字符串
    /// </summary>
    /// <param name="command">若生成成功则返回赋值后的字符串，否则返回出错原因</param>
    /// <returns>是否成功生成指令</returns>
    public abstract bool GetCommand(out string command);
}

public class EditProfile : GaussianProfile
{
    // 提示词
    public string EditPrompt;
    // 分割提示词
    public string SegPrompt;
    
    // g0阶段锚点初始化权重
    public float AnchorWeightInitG0;
    // 一般阶段锚点初始化权重
    public float AnchorWeightInit;
    // 锚点权重乘数
    public float AnchorWeightMultiplier;


    // 颜色锚点损失权重
    public float LambdaAnchorColor;
    // 几何锚点损失权重
    public float LambdaAnchorGeo;
    // 缩放锚点损失权重
    public float LambdaAnchorScale;
    // 透明度锚点损失权重
    public float LambdaAnchorOpacity;
    
    // 最大密集化百分比
    public float MaxDensifyPercent;
    // 密集化开始轮数
    public uint DensifyFromIter;
    // 密集化结束轮数
    public uint DensifyUntilIter;
    // 密集化间隔
    public uint DensificationInterval;
    public override bool GetCommand(out string command)
    {
        if (GetGeneralCommand(out string generalCommand))
        {
            if (string.IsNullOrWhiteSpace(EditPrompt) || string.IsNullOrWhiteSpace(SegPrompt))
            {
                command = "Please input valid prompts";
                return false;
            }
            command = generalCommand + $"system.prompt_processor.prompt={PrepareString(EditPrompt)} system.seg_prompt={PrepareString(SegPrompt)} " +
                      $"system.anchor_weight_init_g0={AnchorWeightInitG0} system.anchor_weight_init={AnchorWeightInit} system.anchor_weight_multiplier={AnchorWeightMultiplier} " +
                      $"system.loss.lambda_anchor_color={LambdaAnchorColor} system.loss.lambda_anchor_geo={LambdaAnchorGeo} system.loss.lambda_anchor_scale={LambdaAnchorScale} system.loss.lambda_anchor_opacity={LambdaAnchorOpacity} " +
                      $"system.max_densify_percent={MaxDensifyPercent} system.densify_from_iter={DensifyFromIter} system.densify_until_iter={DensifyUntilIter} system.densification_interval={DensificationInterval} ";
            return true;
        }

        command = generalCommand;
        return false;
    }
}

public class AddProfile : GaussianProfile
{
    // 重绘指令
    public string InpaintPrompt;
    // 修补指令
    public string RefinePrompt;
    // 缓存文件夹
    private const string CacheDir = Status.CacheDir;
    
    public override bool GetCommand(out string command)
    {
        if (GetGeneralCommand(out string generalCommand))
        {
            if (string.IsNullOrWhiteSpace(InpaintPrompt) || string.IsNullOrWhiteSpace(RefinePrompt))
            {
                command = "Please input valid prompts";
                return false;
            }
            command = generalCommand + $"system.inpaint_prompt={PrepareString(InpaintPrompt)} system.refine_prompt={PrepareString(RefinePrompt)} system.cache_dir={PrepareString(CacheDir)} ";
            return true;
        }

        command = generalCommand;
        return false;
    }
}

public class DeleteProfile : GaussianProfile
{
    // 重绘提示词
    public string InpaintPrompt;
    // 分割提示词
    public string SegPrompt;
    
    // 修复空洞
    public bool FixHoles;
    // 修复区域缩放比例
    public float InpaintScale;
    // 掩码膨胀像素数
    public uint MaskDilate;
    
    // g0阶段锚点初始化权重
    public float AnchorWeightInitG0;
    // 一般阶段锚点初始化权重
    public float AnchorWeightInit;
    // 锚点权重乘数
    public float AnchorWeightMultiplier;
    
    // 颜色锚点损失权重
    public float LambdaAnchorColor;
    // 几何锚点损失权重
    public float LambdaAnchorGeo;
    // 缩放锚点损失权重
    public float LambdaAnchorScale;
    // 透明度锚点损失权重
    public float LambdaAnchorOpacity;
    

    // 最大密集化百分比
    public float MaxDensifyPercent;
    // 密集化开始轮数
    public uint DensifyFromIter;
    // 密集化结束轮数
    public uint DensifyUntilIter;
    // 密集化间隔
    public uint DensificationInterval;

    
    public override bool GetCommand(out string command)
    {
        if (GetGeneralCommand(out string generalCommand))
        {
            if (string.IsNullOrWhiteSpace(InpaintPrompt) || string.IsNullOrWhiteSpace(SegPrompt))
            {
                command = "Please input valid prompts";
                return false;
            }
            command = generalCommand + $"system.inpaint_prompt={PrepareString(InpaintPrompt)} system.seg_prompt={PrepareString(SegPrompt)} " +
                      $"system.fix_holes={FixHoles} system.inpaint_scale={InpaintScale} system.mask_dilate={MaskDilate} "+
                      $"system.anchor_weight_init_g0={AnchorWeightInitG0} system.anchor_weight_init={AnchorWeightInit} system.anchor_weight_multiplier={AnchorWeightMultiplier} " +
                      $"system.loss.lambda_anchor_color={LambdaAnchorColor} system.loss.lambda_anchor_geo={LambdaAnchorGeo} system.loss.lambda_anchor_scale={LambdaAnchorScale} system.loss.lambda_anchor_opacity={LambdaAnchorOpacity} " +
                      $"system.max_densify_percent={MaxDensifyPercent} system.densify_from_iter={DensifyFromIter} system.densify_until_iter={DensifyUntilIter} system.densification_interval={DensificationInterval} ";
            return true;
        }

        command = generalCommand;
        return false;
    }
}