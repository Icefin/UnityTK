using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace UnityTK.Runtime
{
    public class ScaleTextAnimation : UITextAnimationComponent
    {
        [SerializeField] private float _duration = 0.6f;
        [SerializeField] private float _scale = 1.2f;

        public override async UniTask PlayAsync(TMP_Text target, string text = null)
        {
            target.ForceMeshUpdate();
            int count = target.textInfo.characterCount;
            for (int i = 0; i < count; i++)
            {
                LMotion.Create(Vector3.one, Vector3.one * _scale, _duration / 2)
                    .WithDelay(i * 0.02f)
                    .WithEase(Ease.OutBack)
                    .BindToTMPCharScale(target, i);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(_duration + count * 0.02f));
        }
    }    
}
