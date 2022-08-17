using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class SceneSingletonReporter : MonoBehaviour
{
    [SerializeField] private bool isSingleton = true;

    private void Awake()
    {
        if (FindObjectsOfType<SceneSingletonReporter>().Any(x => x != this && x.gameObject.scene == this.gameObject.scene))
        {
            throw new System.InvalidProgramException("Scene "+gameObject.scene.name+" loaded multiple times!");
        }
    }
}
