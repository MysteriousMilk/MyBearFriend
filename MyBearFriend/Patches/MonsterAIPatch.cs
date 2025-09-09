using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBearFriend.Patches
{
    //[HarmonyPatch]
    //public static class MonsterAIPatch
    //{
    //    [HarmonyPostfix]
    //    [HarmonyPatch(typeof(MonsterAI), "CanConsume")]
    //    public static void CanConsume(ref MonsterAI __instance, bool __result, ItemDrop.ItemData item)
    //    {
    //        foreach (var itemDrop in __instance.m_consumeItems)
    //            Jotunn.Logger.LogWarning($"{itemDrop.m_itemData.m_shared.m_name}");
    //    }
    //}
}
