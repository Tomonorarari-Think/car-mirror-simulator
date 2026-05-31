
using UnityEngine;
using UnityEngine.UI;
using CarMirrorSimulator.Core;
using System.Collections.Generic;

namespace CarMirrorSimulator.Manager
{
    /// <summary>
    /// UIボタンによるコントロール切り替えを管理する
    /// </summary>
    public class ControlSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public class ControlEntry
        {
            public Button ActivateButton;
            public Transform Target;
        }

        [SerializeField]
        private List<ControlEntry> entries;

        private List<IControllable> controls;

        private void Awake()
        {
            ResolveControls();
        }

        private void Start()
        {
            RegisterListeners();
        }

        private void ResolveControls()
        {
            controls = new List<IControllable>(entries.Count);
            foreach (var entry in entries)
            {
                controls.Add(entry.Target.GetComponent<IControllable>());
            }
        }

        private void RegisterListeners()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                int index = i;
                entries[index].ActivateButton.onClick.AddListener(controls[index].ActivateControl);

                for (int j = 0; j < controls.Count; j++)
                {
                    if (j == index) { continue; }
                    int other = j;
                    entries[index].ActivateButton.onClick.AddListener(controls[other].DeactivateControl);
                }
            }
        }
    }
}
// --- EOF ---
