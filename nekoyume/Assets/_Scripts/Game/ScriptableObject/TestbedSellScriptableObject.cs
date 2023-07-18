﻿#if UNITY_EDITOR || LIB9C_DEV_EXTENSIONS
using Lib9c.DevExtensions.Model;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TestbedSell", menuName = "Scriptable Object/Testbed/sell",
        order = int.MaxValue)]
    public class TestbedSellScriptableObject : BaseTestbedScriptableObject<TestbedSell>
    {
        // public void OnValidate() {
        //     Debug.Log(Time.time);
        // }
    }
}
#endif
