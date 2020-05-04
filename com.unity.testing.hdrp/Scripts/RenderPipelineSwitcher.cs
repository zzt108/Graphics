using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest
{
    [ExecuteInEditMode]
	public class RenderPipelineSwitcher : MonoBehaviour
	{
	    HDRenderPipelineAsset previousPipeline = null;
	    public HDRenderPipelineAsset targetPipeline = null;

		void OnEnable ()
	    {
	    	if(previousPipeline == null)
	    	{
	        	previousPipeline = (QualitySettings.renderPipeline as HDRenderPipelineAsset);
	    	}
            if (targetPipeline != null && QualitySettings.renderPipeline != targetPipeline)
            {
                QualitySettings.renderPipeline = targetPipeline;
            }
        }
        void Update()
        {
            if (previousPipeline == null)
            {
                previousPipeline = (QualitySettings.renderPipeline as HDRenderPipelineAsset);
            }
	        if(targetPipeline != null && QualitySettings.renderPipeline != targetPipeline)
	        {
                QualitySettings.renderPipeline = targetPipeline;
	        }
		}

		void OnDisable()
		{
            QualitySettings.renderPipeline = previousPipeline;
		}
	}
}
