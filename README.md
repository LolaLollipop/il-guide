# il-guide

## first steps

### introduction to IL and transpilers
as you might know, c# (like other .net languages) is compiled. this means that the code you write isn't converted directly to machine code (instructions to the hardware and CPU). instead, c# is compiled into something called IL (intermediate language), which is then read as machine code. 

the problem is that since the code is compiled into IL and no longer is c#, you can't just insert c# code into it. you need a way to insert new IL (or remove/modify/replace old IL). this is where a **harmony transpiler** comes in. <ins>a transpiler receives the old instructions and gives new ones, with all of your modifications in it</ins>. transpilers are how many exiled events and many plugins work, as they give significantly more control than prefixes and postfixes, and have a much lower chance of breaking other plugins. 

however, in order to write our first transpiler, we first must understand the basics of il.

### IL basics
structually, il is similar to most other programming languages. <ins>all of the instructions are in a list, and are read top to bottom</ins>. 

for example, the following c# code:
![Csharp](https://github.com/Ruemena/il-guide/assets/135553058/10e965fb-41c4-4ea8-851e-ac33b399e9eb)

would be this in IL:
![IL](https://github.com/Ruemena/il-guide/assets/135553058/a436060d-9be2-4f73-922d-10a2e48b70ae)

you can probably connect a bit of the c# code to the il code, especially with the methods. 

below you can see an example of an instruction. each instruction in IL has an index (green), an offset (blue), an opcode (red), and sometimes an operand (purple).

![instruction](https://github.com/Ruemena/il-guide/assets/135553058/a6f936c2-8b9e-4563-bc8b-4f645bf86295)

the index is where in the list of instructions this specific instruction is located. the offset acts as a reference to the specific instruction (this will become important later).  <ins>the **opcode** is probably the most important part. this says what specific instruction will be happening</ins>. for example, the opcode to say to add two numbers is `add`. often, you'll need to pass parameters to the opcode. for example, if you wanted to call a method using the `call` opcode, you would need to pass a reference to a method. this is done through the operand - somewhat analogous to providing arguments to a method call, except that it's fixed and can never change.

in all of this, data needs a way to be passed around. unlike c#, this isn't done through named variables. instead, data is passed around through the **stack**, also referred to as the evaluation stack (it means the same thing). the stack is a last in, first out data structure capable of holding any type - in other words, <ins>if you add something and then immediately get an item from the stack, the item that you just added would be returned</ins>. most opcodes interact with the stack in some way, either taking a certain number of values from it ("popping"), adding a certain number of values to it ("pushing"), or doing both. 
> [!NOTE]
> throughout this guide, i'll also be referring to adding something to the top of the stack as pushing something onto the stack and loading something onto the stack. these terms are all interchangeable!

a c# implementation of the stack can visualized like this:
```csharp
public class Stack<T>
{
    private List<T> items = new();

    public void Push(T item)
    {
        items.Insert(0, item);
    }

    public T Pop()
    {
        var item = items[0];
        items.RemoveAt(0);
        return item;
    }
}
```
with this example, the stack used in IL is a `Stack<object>`, since it can hold anything. there's something important to note about the stack - there's no way to get the item from the top of the stack without removing it (unless you use the Dup opcode). more advanced ways of manipulating the stack will be discussed later.

let's go back to the `add` opcode discussed earlier. it gets the top two things on the stack and adds them together, erroring if they both aren't numbers. so, if the stack looks like:
```
- Top -
int 1
int 3
float 5
string "Hello!"
```
and an `add` opcode is encountered, it will remove the top two items from the stack and produce a new one, turning it into:
```
- Top -
int 4
float 5
string "Hello!"
```
if you still don't understand, that's okay - we can go back to the `Stack<object>` implementation discussed earlier. with that, we can visualize our example as
```csharp
// setting up
Stack<object> stack = new();
stack.Push("Hello!");
stack.Push(5f);
stack.Push(3);
stack.Push(1);
// add opcode
int first = (int)stack.Pop();
int second = (int)stack.Pop();
stack.Push(first + second);
```
now that we have an understanding of the stack, we can discuss some of the most important and common opcodes so that we can create our first transpiler. 
### REVIEW!!
- il is composed of a series of instructions
- each instruction has an index, an offset (reference to it), opcode (saying what it does), and sometimes an operand (fixed parameters)
- data is passed around in a stack that contains any type - a series of values where when an item is added it's put on the top and becomes the first item to be removed
- opcodes interact with the stack by adding items or removing them
## our first transpiler
### background info
while there are many opcodes with varying levels of importance (some you'll almost never encounter), 4 big ones are going to be discussed. 

the first one is `ldarg`, which actually encompasses a number of very similar opcodes. if in a *static* method, ldarg pushes an argument of the method onto the stack, based on the zero based index. for arguments 1-4, you have the opcodes Ldarg_0, Ldarg_1, Ldarg_2, and Ldarg_3. however, if you need to access an argument beyond the fourth one, you need to do the `Ldarg_S` opcode, and pass the index as the operand. so, if you're inside `static void Example(int number, float anotherNumber, string notANumber, string anotherNotNumber, List<object> definitelyNotANumber)`, `ldarg_1` would load the float anotherNumber onto the stack, and `ldarg_S 4` loads the list definitelyNotANumber onto the stack (4 being the operand). 

things are slightly different for instance methods. an instance method is any method called on an instance of a class - so, for example, Player.Kill is an instance method. <ins>in instance methods, the first argument is the current instance, equivalent to `this`</ins>. therefore, `ldarg_0` loads the current instance onto the stack. accessing arguments is done through the usual way, except that you need to simply increment the index of the argument by one. so, if you're inside the instance method `void InstanceExample(int newNumber, string newNotNumber)` which is itself inside the class `ExampleClass`, `ldarg_0` loads the current instance of `ExampleClass` and `ldarg_1` loads the int newNumber onto the stack.

the second of these actually comes as a pair - `call` and `callvirt`. both of these accomplish something similar - a method call. the main difference between `call` and `callvirt` is that <ins>`call` is used to call static methods and `callvirt` is used to call `instance methods`</ins> that are inside classes. when you call an instance method using `callvirt`, the top of the stack is popped for an instance to call the method on. the technical differences between them are complicated and convoluted, but generally unnecessary to know. 

when using either of these methods, the arguments for the method are popped from the stack in order from left to right. so if you called `void function DoSomething(int number, float decimalNumber, string funkyString)`, first number, then decimalNumber, and finally funkyString would be popped from the stack. if the method has a return value, then it's pushed onto the stack. something important to note is that in il, default values for methods don't exist - you *must* provide a value for every parameter. 

both of these methods are also used to call the property getter for *properties*, which brings us into our next opcode and an important distinction.

in c#, there actually exists two different kinds of values inside classes. while they are both accessed through the dot operator (like `Example.ExampleValue`) in c#, when you look under the hood they are both accessed very differently. the first of these are **fields**. fields are essentially raw variables inside of classes, defined without any accessors (get or set). so all of these are fields:
```csharp
public class LotsOfFields
{
    public o string Constant = "Hello!";
    public int Number;
    private readonly float H = 1;
    private static List<float> List = new();
}
```
when we want to load a field onto the stack, we use `ldfld` for fields on instances and `ldsfld` for fields on static classes, passing a reference to the field as our operand. for our LotsOfFields class, we would need to do `ldfld LotsOfFields.Number` to access `Number` and `ldsfld LotsOfFields.List` to access `List`. just like with `callvirt` `ldfld` pops the top of the stack is popped for an instance to call the method on

the other type of values inside classes are properties. properties wrap around a field and expose accessors (get and set). properties are defined with accessors. so, all of these are properties:
```csharp
public class LotsOfProperties
{
    public string String { get; } = "Hello!";
    public int Number => Math.Max(1, 2); // equivalent to public int Number { get { return Math.Max(1, 2); } }
    public float BigFloat
    {
        get
        {
            return 595159140f;
        }
        set => BigFloat = value; // equivalent to set { BigFloat = value }
    }
}
```
the big difference is that when you're accessing a property, you're actually calling a method that returns a value. so, we have to use `call` or `callvirt` (for properties on static classes and instances respectively). so to access String in our example, we do `callvirt get_String()`. something important to note is that this won't actually be how we'll access properties when we write our transpiler using harmony (since we can't directly reference get/set accessors), this is just how it's done in actual il. 
## time to ACTUALLY write the transpiler
our mission throughout this will be to <ins>make it so that tutorials can hear the scp chat</ins>. 

first, we need to set up harmony for our plugin. if you already know how to do this, you can skip this step.

somewhere, in your plugin's main class, define a Harmony class. field or property doesn't matter. you don't have to create it yet - you can do that when the plugin is enabled. you'll also want to create a harmony id, which is a unique string used to identify your plugin when patching and unpatching. so, something like:
```csharp
public sealed class ExamplePlugin : PluginConfig
{
    // ...
    // ...
    private Harmony _harmony;
    private string HarmonyId { get; } = "This can be whatever you want!";
    // ...
}
```
then, in OnEnabled for the plugin, you'll want to create the new Harmony instance (if you haven't already), providing the HarmonyId as a parameter. then, call the PatchAll() method on the harmony instance. make sure that you're running UnpatchAll() in your OnDisabled, too. so, something like:
```csharp
public sealed class ExamplePlugin : PluginConfig
{
    // ...
    private Harmony _harmony;
    private string HarmonyId { get; } = "This can be whatever you want!";
    // ...
    public override void OnEnabled() {
        _harmony = new(HarmonyId);
        _harmony.PatchAll()
        // ...
    }
    public override void OnDisabled() {
        _harmony.UnpatchAll()
        // ...
    }
}
```

all done? great! the next step will be to figure out what we need to patch. we don't need to do this blindly, luckily. there are many tools that you use to look at the decompiled code of the game. for this, we'll be using dnspy. again, if you already know how to do this, you can skip this step. first, you'll want to get the latest release of dnspy from [here](https://github.com/dnSpy/dnSpy/releases/latest). i assume you already know how to download something, so we can skip that. open up dnspy, and go to File -> Open. open up the Assembly-CSharp.dll for sl - you can get it from one of your server folders by going to SCPSL_Data/Managed/, among other places.

a big part of transpilers is figuring out *what* you need to patch to accomplish your goal. this mostly involves just looking around for a while. we'll skip the frustration of that. we'll first want to search by doing `Ctrl + Shift + K`. search for `VoiceTransceiver.ServerReceiveMessage`. this method determines if a player should receive a voice message and the channel that the voice message should be sent on - perfect for our situation!

we'll want to create a new *static* class somewhere - you can name it whatever you'd like. put a HarmonyPatch attribute on the class (right above the definition), passing the type of the VoiceTransceiver class and the name of the VoiceTransceiver.ServerReceiveMessage. so, it should like:
```csharp
[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
internal static class MyFirstTranspiler {

}
```
we'll now need a transpiler method. this is a static method that takes in the original il instructions as an `IEnumerable<CodeInstruction>` (with the parameter name `instructions` and returns new the instructions, also as an `IEnumerable<CodeInstruction>`. now, at this point, you have two options:
- name the method whatever you'd like and add the `[HarmonyTranspiler]` attribute
- name the method `Transpiler` exactly
you can also do both of these, if you really want to. it should look something like this:
```csharp
[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
public static class MyFirstTranspiler {
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {

    }
    // OR
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ACoolTranspiler(IEnumerable<CodeInstruction> instructions) {

    }
}
```
sometimes, you may see `ILGenerator generator` included in the parameters for the transpiler method. this becomes important with branching statements, but for now you don't have to worry about it.

one of the neat things about transpilers is that there is a ton of ways to make one. the way discussed in this is the one most commonly used (especially for exiled), but don't fret if you see one that's different (or make one that's different). our first step will be making a list from these instructions. we want this to be a `List<CodeInstruction>`, as codeinstruction is the class that represent each il instruction. however, unlike what you'd might expect, we're not just going to create a new instance of `List<CodeInstrucition>` ourselves. instead, we'll be 'renting' a list from `NorthwoodLib.Pools`'s ListPool. this is mostly for when we want to create a temporary list, especially from another list. there are a few advantages to this compared to just creating one ourselves, mostly being that it allows for better management and resource allocation. for now, don't worry too much about it. so, it should look something like this:
```csharp
List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);
```
our next step will be figuring out where to put our new instructions. go back to dnspy, and hover over the ServerReceiveMessage method. right click and click "Edit IL Instructions". we won't actually be editing il instructions using this - this just lets us look at the il of the code. you can probably see that there's a TON of code - way too difficult to sort through in il. close the il view, and look back at your method. we want to be able to edit the channel, setting it to `RoundSummary` (which is identical to scp chat for people) if the player listening is a Tutorial and the speaker is an SCP. there's a specific line of code that's perfect for this:
```csharp
VoiceChatChannel voiceChatChannel2 = voiceRole2.VoiceModule.ValidateReceive(msg.Speaker, voiceChatChannel);
```
this method validates the channel - if the listener shouldn't be able to hear the message, it sets it to None (which will not send the message to the listener's client). so, we want to put it right below this, as that means we can add new functionality to it. hover over this specific line, and right click -> Edit IL Instructions again. the problem right now is that we don't know where specifically in il we should put our new instructions. we'll want to do it after the ValidateReceive message is called, but before it's popped off the stack. there's one specific instruction that's a perfect fit for this: `callvirt VoiceModuleBase.ValidateReceive` (the second to last line). since the VoiceChatChannel is still on the stack if we insert right after this, we can edit it easily. 

while we now know where we want to put our new instructions, we can't just add our new instructions to wherever dnspy says the index of the instruction is. this would basically guarantee our transpiler would break if a single new instruction is inserted, as we'd be inserting in the wrong place. instead, we will want to dynamically find this specific instruction and add an offset. this isn't as hard as it sounds, luckily. we'll use the FindIndex method on the list, which takes in a method that decides whether or not it's our match and returns the index of that. we'll want to check if the opcode of the instruction is `callvirt` and if the operand is `VoiceModuleBase.ValidateReceive`. since the operand can be anything, however, we'll have to cast it to MethodInfo (which represents the metadata and reference for a certain method call) and use the Method method to a create a new MethodInfo for a call to `VoiceModuleBase.ValidateReceive`. if these two are equal, it's our instruction! so, we'll do something like:
```csharp
int index = newInstructions.FindIndex(instruction =>
            instruction.opcode == OpCodes.Callvirt
            && (MethodInfo)instruction.operand == Method(typeof(VoiceModuleBase), nameof(VoiceModuleBase.ValidateReceive)));
```
you'll see a lot of the XXXXX(typeof(XXXXX), nameof(XXXX)) syntax, as it allows for us to pass the metadata and reference to a certain field, property, method, etc. then, we'll want to add 1 to the index, so that our code executes right after the instruction. it should look something like:
```csharp
int index = newInstructions.FindIndex(instruction =>
            instruction.opcode == OpCodes.Callvirt
            && (MethodInfo)instruction.operand == Method(typeof(VoiceModuleBase), nameof(VoiceModuleBase.ValidateReceive)));
index += 1;
```
now we know where to put our instructions! we'll want to insert a list (technically an array) of our new instructions. fortunately, `List` has an `InsertRange` method that we can use to easily insert multiple items into a list. it takes in an index and a new `IEnumerable`. so we can do something like:
```csharp
newInstructions.InsertRange(index, new[]
{
 // our instructions go here!
});
```
right now, this inserts nothing into the list of instructions. unsurprisingly, this won't do anything. first, however, we actually have to figure out what we need. real quick, let's make a method that takes in the voice message's channel (from the ValidateReceive), the speaker for the voice message, and the current listener, returning `RoundSummary` if the channel is SCP chat, the speaker is an scp, and the listener is a Tutorial. something like this:
```csharp
private static VoiceChatChannel TutorialHearSCPs(VoiceChatChannel channel, ReferenceHub speaker, ReferenceHub listener) {
    if (speaker.GetRoleId() == RoleTypeId.Tutorial && listener.IsSCP()) return VoiceChatChannel.RoundSummary; else return channel;
}
```
make sure this method is static, otherwise it won't work! now, we have this method, but we need to provide all of the parameters to it. let's go back to dnspy. if you look at where the code is being executed, VoiceChatChannel is already on the top of the stack, so we don't have to worry about writing new code for it. how are we going to get the speaker and listener, though? this is where it's a good idea to look around the il code for the original method for something that does something similar to what we're doing. we can see that it's getting the speaker for the message by doing msg.Speaker, and msg is one of the arguments for the method. if we look at the code for the line 
```csharp
IVoiceRole voiceRole = msg.Speaker.roleManager.CurrentRole as IVoiceRole;
```
we can see that it does
```
ldarg.1
ldfld (ReferenceHub) VoiceMessage.Speaker
```
let's dissect this a little bit. as discussed previously, `ldarg` loads one of the arguments onto the stack - since this is a static method and it's `ldarg_1`, it's going to load the second argument for this method onto the stack. and, since Speaker is a field, we pop the VoiceMessage and load the speaker of the VoiceMessage onto the stack by doing `ldfld VoiceMessage.Speaker`. we now have to convert this into CodeInstructions. go back to our InsertRange, and let's create two new CodeInstructions. 
the first is pretty simple:
```csharp
new CodeInstruction(OpCodes.Ldarg_1),
```
the second, however, is slightly more complicated. we have to pass a reference to a field as an operand. the second parameter for the CodeInstruction constructor is the operand. remember when we did `Method(typeof(VoiceModuleBase), nameof(VoiceModuleBase.ValidateReceive))`? we can do that for fields, too! it should look something like:
```csharp
new CodeInstruction(OpCodes.Ldarg_1),
new CodeInstruction(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
```
all that's left is to get the listener, and then call our own method. once again we can turn to the c# code and look at the il for a hint on how to do this. let's look at:
```csharp
IVoiceRole voiceRole2 = referenceHub.roleManager.CurrentRole as IVoiceRole;
```
`referenceHub` in this scenario represents our listener. in il, we can see that it does:
```
ldloc.3
ldfld ReferenceHub.RoleManager
```
`ldloc` is a new opcode that we're going to discuss. while the stack is the only way to pass data to opcodes, there are other ways to store data inside il. one of the most common is through **local variables**. inside a method there are 256 different 'slots' for local variables, and just like arguments are indexed at 0. we can store a value into a local variable by doing `stloc_0`, `stloc_1`, `stloc_2`, or `stloc_3` for local variables 1-4 and `stloc_s` with the index as the operand for other local variables. this will pop the value on the top of the stack and put it into that slot. then, in a similar fashion, we can access it by doing `ldloc_0`, `ldloc_1`, `ldloc_2`, or `ldloc_3` for local variables 1-4 and `ldloc_s` with the index as the operand for other local variables. this will load the local's variable onto the stack. like `ldarg`, it won't remove the local variable's value, so you can do it as much you'd like. 

this makes sense, as if you look right above, you can see that it does
```
call HashSet<ReferenceHub>.get_Current()
stloc_3
```
in other words, it's storing the current reference hub as the third local variable. when we get to our instructions, it's fortunately still there, so we can use it easily! we now do:
```csharp
new CodeInstruction(OpCodes.Ldarg_1),
new CodeInstruction(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
new CodeInstruction(OpCodes.Ldloc_3),
```
all that's left is to call our function. since our TutorialHearSCPs method is static, we'll use `call`. and, just like how we created a MethodInfo using harmony's Method method to check to see if the instruction was a match, we're going to do that to pass it as an operand to our call opcode. this will look something like:
```csharp
new CodeInstruction(OpCodes.Ldarg_1),
new CodeInstruction(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
new CodeInstruction(OpCodes.Ldloc_3),
new CodeInstruction(OpCodes.Call, Method(typeof(MyFirstTranspiler), nameof(MyFirstTranspiler.TutorialHearSCPs)))
```
we're all done creating our new instructions! all that's left is a bit of cleanup and returning the values. our code right now looks like:
```csharp
[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
public static class MyFirstTranspiler {
    private static VoiceChatChannel TutorialHearSCPs(VoiceChatChannel channel, ReferenceHub speaker, ReferenceHub listener) {
        if (speaker.GetRoleId() == RoleTypeId.Tutorial && listener.IsSCP()) return VoiceChatChannel.RoundSummary; else return channel;
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
    }
}
```
now we'll want to return `newInstructions`, since it has all of our instructions we just added. we can't just do return, as we have to clean up the ListPool<CodeInstruction>. instead, we do `yield return` for each instruction in `newInstructions`. `yield return` returns one item to create a new iterator. you can kind of imagine this like creating a new IEnumerable, adding a new item to it every time `yield return` is called, and then returning that new IEnumerable. it's slightly more complicated than that, but that's irrelevant for this. after we do that, we then return our List to the pool, cleaning it up. after all of that, our code looks like this:
```csharp
[HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
public static class MyFirstTranspiler {
    private static VoiceChatChannel TutorialHearSCPs(VoiceChatChannel channel, ReferenceHub speaker, ReferenceHub listener) {
        if (speaker.GetRoleId() == RoleTypeId.Tutorial && listener.IsSCP()) return VoiceChatChannel.RoundSummary; else return channel;
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
```
we're done! this was a long journey, but we've successfully created our very first transpiler. 
