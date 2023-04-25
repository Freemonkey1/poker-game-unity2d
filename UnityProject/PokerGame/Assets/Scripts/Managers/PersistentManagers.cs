using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Klasa, kt�ra zapewnia nie niszczenie GameObject'u,
// do kt�rego zosta�a do��czona
public class PersistentManagers : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
