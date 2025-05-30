using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UnityTK.Runtime
{
    public class TypeWriteTextAnimation : UITextAnimationComponent
    {
        [SerializeField] private float _charInterval = 0.04f;

        public override async UniTask PlayAsync(TMP_Text target, string text = null)
        {
            if (text == null) text = target.text;
            target.text = "";
            for (int i = 0; i < text.Length; i++)
            {
                target.text += text[i];
                target.ForceMeshUpdate();
                await UniTask.Delay(TimeSpan.FromSeconds(_charInterval));
            }
        }
    }
}
