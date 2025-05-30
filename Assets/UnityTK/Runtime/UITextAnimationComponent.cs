using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UnityTK.Runtime
{
    public abstract class UITextAnimationComponent : Component
    {
        public abstract UniTask PlayAsync(TMP_Text target, string text = null);
    }    
}
