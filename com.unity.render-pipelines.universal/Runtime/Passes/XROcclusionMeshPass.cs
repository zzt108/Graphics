namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the XR occlusion info into the given depth stencil buffer when XR is enabled.
    ///
    /// </summary>
    public class XROcclusionMeshPass : ScriptableRenderPass
    {
        RenderTargetHandle m_TargetDepthTarget;
        const string m_ProfilerTag = "XR Occlusion Pass";
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

        public XROcclusionMeshPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        public void Setup(in RenderTargetHandle targetDepth)
        {
            m_TargetDepthTarget = targetDepth;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // XRTODO: We need a way to specify the depth slice of the target depth here. Current URP scriptable renderer doesn't track depth slice and we need to add one.
            ConfigureTarget(m_TargetDepthTarget.Identifier(), m_TargetDepthTarget.Identifier());
            ConfigureClear(ClearFlag.None, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.xr.enabled)
                return;
            
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                renderingData.cameraData.xr.StopSinglePass(cmd);
                renderingData.cameraData.xr.RenderOcclusionMeshes(cmd, m_TargetDepthTarget.Identifier());
                renderingData.cameraData.xr.StartSinglePass(cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
