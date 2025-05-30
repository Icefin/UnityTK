using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace UnityTK.Runtime
{
    public class UITextAnimation : MonoBehaviour
    {
        [SerializeField] private TMP_Text _targetText;
        private readonly List<UITextAnimationComponent> _components = new();

        public void AddComponent(UITextAnimationComponent comp)
        {
            if (!_components.Contains(comp))
                _components.Add(comp);
        }

        public void RemoveComponent<T>() where T : UITextAnimationComponent
        {
            _components.RemoveAll(c => c is T);
        }

        public async UniTask PlayAllAsync(string text = null)
        {
            foreach (var comp in _components)
            {
                await comp.PlayAsync(_targetText, text);
            }
        }
    }
}
