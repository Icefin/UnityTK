using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace UnityTK.Runtime
{
    public class BounceTextAnimation : UITextAnimationComponent
    {
        [SerializeField] private float _duration = 0.5f;
        [SerializeField] private float _strength = 30f;

        public override async UniTask PlayAsync(TMP_Text target, string text = null)
        {
            target.ForceMeshUpdate();
            int count = target.textInfo.characterCount;
            for (int i = 0; i < count; i++)
            {
                // LitMotion Punch로 캐릭터별 Y축 바운스
                LMotion.Punch.Create(Vector3.zero, Vector3.up * _strength, _duration)
                    .WithDelay(i * 0.03f)
                    .BindToTMPCharPosition(target, i);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(_duration + count * 0.03f));
        }
    }
}
