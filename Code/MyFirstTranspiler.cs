[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
public static class MyFirstTranspiler {
    private static VoiceChatChannel TutorialHearSCPs(VoiceChatChannel channel, ReferenceHub speaker, ReferenceHub listener) {
        if (listener.GetRoleId() == RoleTypeId.Tutorial && speaker.IsSCP()) return VoiceChatChannel.RoundSummary; else return channel;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);
        int index = newInstructions.FindIndex(instruction =>
            instruction.opcode == OpCodes.Callvirt
            && (MethodInfo)instruction.operand == Method(typeof(VoiceModuleBase), nameof(VoiceModuleBase.ValidateReceive)));
        index += 1;

        newInstructions.InsertRange(index, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
            new CodeInstruction(OpCodes.Ldloc_3),
            new CodeInstruction(OpCodes.Call, Method(typeof(MyFirstTranspiler), nameof(TutorialHearSCPs)))
        });

        foreach (CodeInstruction instruction in newInstructions)
            yield return instruction;

        ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}
