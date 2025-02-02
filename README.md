# il-guide
this is v2 of a guide on CIL and how to write transpilers. this is meant to be very gentle, and many of the more technical or abstracted concepts will be explained. however, i highly recommend that you have a good grasp on c# and harmony (prefixes and postfixes).

other resources:
- [sharplab](https://sharplab.io/): this can convert c# to il in real time, which is extremely helpful for understanding how something may be compiled into il.
- [microsoft's list of opcodes](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes?view=net-8.0) every opcode, and their expected operand, can be found here.
- cil [partition 1](https://download.microsoft.com/download/7/3/3/733ad403-90b2-4064-a81e-01035a7fe13c/ms%20partition%20i.pdf) [partition 2](https://download.microsoft.com/download/7/3/3/733ad403-90b2-4064-a81e-01035a7fe13c/ms%20partition%20ii.pdf) [partition 3](https://download.microsoft.com/download/7/3/3/733ad403-90b2-4064-a81e-01035a7fe13c/ms%20partition%20iii.pdf): these are official microsoft documents on the implementation of CIL. while these are very technical, they can be helpful once you have a good grasp on il.


## introduction to CIL
### what even is CIL?
you're probably familiar with compiling C# code. when you build a project, it generates a dll that you can then use. inside of this dll is Common Intermediate Language (CIL, also called IL) code. this code is then ran at runtime and converted into machine code, which is the actual instructions that the hardware runs. this enables C# code to target a wide range of architectures and communicate with other languages that also use CIL, such as F# and Visual Basic.
### instructions
CIL contains all of the information necessary to run C# code: information about classes, the signatures of methods, metadata necessary for reflection, etc. the most important, and what transpilers target, is the code within methods. each method is composed of a list of **instructions**, executed from top to bottom. these instructions, just like statements in C#, says what should be executed and how. each instruction is composed of 1 or 2 parts:
- the first is the opcode. an opcode identifies what kind of instruction is going on - for example, it might say to add two numbers. in a sense, opcodes are essentially an enum; it doesn't actually say any information about the numbers that are being added, it just says "at this point, add two numbers".
- some opcodes can also take an operand (it's either they take an operand or don't take an operand, they're never optional). the type of the operand varies based on the opcode, and specifies what the opcode should act upon. in a sense, this is somewhat comparable to arguments being passed to a method, but unlike arguments to a method, this is fixed at compile time and never changes during runtime.

for example, let's take a look at a very simple opcode. one of the most basic opcodes used is the `call` opcode, which is used to call a static method, though it may occassionally be used under certain circumstances. its operand is the static method that should be called. not all instructions have operands: the `add` opcode takes no operands.

### the stack
at this point, you might be a little confused. if we have the `call` opcode, and its operand is a method, and that operand can never ever change, how do we pass arguments? in fact, how do we pass data between instructions at all? this is done through the **evaluation stack**, often just shorted to the stack. every method execution has its own stack, and like the name suggests, this uses the stack data structure as the basis of how data is transferred. essentially, it's a list of values which you can either push a value onto, or pop and get the most recently added value. 

instructions interact with the stack in various ways, pushing values onto it and popping values from it. note that the stack is capable of holding any type and the types on the stack can change, but the number of values on the stack and their types at any given point are fixed. in other words, it's not possible to push/pop a variable number of items from the stack based on a runtime value or change the types of values on the stack based on a runtime value.

as an example, when calling an method using `call`, it pops the arguments that should be used for the method call from the stack (in the order they are declared in the method, from left to right), calls the method, and the pushes the return value of the method onto the stack (if it doesn't return void).
### primitives
while c# has a wide variety of primitives (basic data types like ints and strings), the primitives stored on the stack are quite restricted. they can either be:
- `int`, made up of 4 bytes (i4)
- `long`, made up of 8 bytes (i)
- `float`, made up of 4 bytes (r4)
- `double`, made up of 8 bytes (r8)
many opcodes deal with these types, so it's good to know them. it's also important to know that if a primitive is smaller than one of these types, it'll be *widened* into them. so if you use a byte, it'll actually only ever be stored on the stack as an int.

if we want to push one of these values onto the stack, CIL provides opcodes to do so, each starting with ldc (load constant).
- for `int`s 0-8, we can use the opcodes `ldc.i4.X`, where X represents the number to push onto the stack. for example, to load 4 as an int onto the stack, we can do `ldc.i4.4`.
- to push -1 onto the stack, you can use the `ldc.i4.m1` instruction.
- for other positive `int`s, you can use the opcode `ldc.i4` and then have the `int` you want to push onto the stack as the operand.
- to push a `float`, `double`, or `long`, you can use `ldc.r4`, `ldc.r8`, and `ldc.i8` respectively, with the number as the operand.

something important to note is that in CIL, `bool`s are represented as `int`s (0 being false and 1 being true). additionally, enums are represented as their underlying type. by default, this is `int`.

lastly, we can use the `ldstr` opcode with a string as an operand to push that string onto the stack.
### tying it together
so far, we know instructions, the stack, and primitives of il. let's combine them! here's some very simple cil code.
```cs
float pi = 3.14159;
float goldenRatio = 1.61803;
Console.WriteLine(Math.Max(pi, goldenRatio));
```
now, let's see this in il and walk through it step by step. the left represents the index (in the list of instructions, represented as hexadecimal), the middle is the opcode, and the right is the operand.
```
0 | ldc.r4 | 3.14159
1 | ldc.r4 | 1.61803
2 | call     | Math.Max(float, float)
3 | call     | Console.WriteLine(float)
```
step by step, our stack after each instruction looks like:
```
1: [ float ]
2: [ float, float ]
3: [ float ]
4: [ ]
```
#### peeking at il yourself
if you haven't already downloaded a tool like [dnSpy](https://github.com/dnSpy/dnSpy), now is a great time to do so. assuming you have dnSpy, you can look at the IL of any method by hovering over the code of the method and clicking "Edit IL Instructions." from here you can take a look at behind the scenes of any method. at this point, i recommend taking a look at a simple method and pick out what you can understand.
## more fundamentals 
by now, you should hopefully have a good grasp on the stack, instructions, and a couple opcodes. this part is where where things get slightly more confusing, as you can no longer rely on c# hiding away niches for you.
### more with methods
earlier, i mentioned that `call` was used for caling *static* methods. for calling instance methods (i.e. non-static methods), you can use `callvirt`. the "virt" in `callvirt` stands for virtual, and indicates that it factors in overrides (methods don't actually have to be declared virtual to be called by it). notably, callvirt pops the instance to call the method on, and then pops the arguments of the method. so, in cil, this:
```cs
list.Add(5);
```
might look like this:
```
// assuming is already on the stack
0 | ldc.i4.5 |
1 | callvirt | List<int>.Add
```

now, to access the arguments of the currently executing method, you can use one of the `ldarg` opcodes. similar to `ldc.i4`, there's a few different ways to access the argument at a specific index (in the order they are declared). arguments are zero-indexed, so:
- for the first, second third, and fourth arguments, you can use `ldarg.0`, `ldarg.1`, `ldarg.2`, and `ldarg.3` (without any operands), respectively.
- for any arguments with an index less than 256, you can use `ldarg.s`, with the index as the operand.
- if you somehow need to access an argument outside of that range, you can use `ldarg`, also with the index as the operand.

> [!NOTE]  
> you'll often see opcodes that end with `.s`. this is called the *short form*. these opcodes have a single byte as an operand, as opposed to their longer forms, which can reduce the file size and speed up the IL being converted to machine code. these are not aliases!

something important to note is that for instance methods, **the first argument (`ldarg.0`) will be the current instance, basically equivalent to `this`**. the normal declared arguments will be effectively shifted to the right and have their index incremented.

let's take a look at an example:
```cs
public class ClassWithTwoMethods
{
    public void DoSomething(string stringParam, int anotherParam, int yetAnotherParm, List<int> oneMoreParam, StringBuilder justKiddingAnother)
    {
        // body omitted
    } 

    public static bool DoSomethingStatically(int intInStaticMethod, string stringInStaticMethod)
    {
        // body omitted
    } 
}
```
for DoSomething, `ldarg.0` would be `ClassWithTwoMethods`, `ldarg.1` would be `int` (`anotherParam`), `ldarg.2` would also be `int` (yetAnotherParm), `ldarg.3` would be `List<int>`, and `ldarg.s 4` would be `StringBuilder`. inside of DoSomethingStatically, `ldarg.0` would be `int` and `ldarg.1` would be string.

one more thing: the `ret` opcode. this is basically identical to `return` in c#, with a few caveats. firstly, the last instruction of every method body must be `ret`, even if they don't return a value[^1]. secondly, if the method returns a value, that must be the only thing on the stack when a `ret` is encountered; otherwise, the stack must be empty when `ret` is encountered.
### branches
branches are how you perform logic like if statements. they're a set of opcodes that can "jump" to another instruction, often conditionally. "jumping" essentially sets the currently executing instruction to a different one and continues the code flow as usual from that instruction. this all branch opcodes are identified by `br` at the start, and most have a short form (only capable of jumping to instrutions at most 127 instructions away). the actual operand of a branch opcode as in cil is an offset (for example, 50 would jump to the instruction 50 instructions ahead), but for clarity and ease of use we will represent it as the index of the instruction to jump to (as does dnspy and harmony).

let's take a look at an example:
```cs
public static void WorldMessage(bool hello)
{
    if (hello)
    {
        Console.WriteLine("Hello, world");
    }
    else
    {
        Console.WriteLine("Goodbye, world");
    }
}
```
the il of this could look something like:
```
0 | ldarg.0   |
1 | brfalse.s | 5
2 | ldstr     | "Hello, world"
3 | call      | Console.WriteLine
4 | ret       | 
5 | ldstr     | "Goodbye, world"
6 | call      | Console.WriteLine
7 | ret       |
```
`brfalse.s` here is the short form of `brfalse`. essentially, it pops a value from the stack: if the value is `false` (if a bool), `null` (if a reference), or `0` (if a primitive number), it jumps to the provided instruction (in our case, instruction at index 5). otherwise, it continues executing as normal. as you might expect, there's also a `brtrue.s` and `br.true`, branching if the value is `true` (if a bool), not `null`, (if a reference), or not `0` (if a primitive number).

another important branching instruction that you might encounter is `br` (or its short form, `br.s`). this instruction unconditionally (i.e. always) jumps to the given instruction. while this might seem useless, it's often a convenient way to prevent extremely tangled control flow logic.

finally, there's an extremely important caveat for branches. somewhat similar to `ret`, if a branch pops a value from the stack (e.g. `brfalse`), that value must be the only thing on the stack; otherwise (e.g. `br`), there must be nothing on the stack. this also means that you can't jump to an instruction with different types on the stack. this means that we *can't* rewrite our `WorldMessage` method like this:
```
0 | ldarg.0   |
1 | brfalse.s | 4
2 | ldstr     | "Hello, world"
3 | br.s      | 6
4 | ldstr     | "Goodbye, world"
6 | call      | Console.WriteLine
7 | ret       |
```
### locals
the biggest issue with the stack is that reordering it is very difficult. if you have variables that are consistently used, it becomes very difficult to just keep everything on the stack and do nothing but push and pop to ensure that what you currently want is on the top of the stack. it would be very nice if there was a way to store things similar to a normal C# variable, so that you can use it at any time without a complicated series of instructions.

now, the stack is the only way of passing data between instructions. but it's far from the only way to store data in CIL code. instead, you can use local variables. these are typed "slots" that you can store values in and load the value later. if they sound exactly like C# variables, they should. it should be noted that all local variables are declared by data present in the method's CIL, not by any opcodes. this notably means that a local variable will always be the same type, no matter where you are in the method body.

each local variable is referenced by the specific index of that variable. to pop a value from the stack and store it in a local variable, you can use `stloc`, with the index of the local variable is the operand. to push a value from a local variable onto the stack, you can use `ldloc` where the operand is expectedly the local variable's index. note that `ldloc` does not clear the local variable: you can store a value in there and `ldloc` it multiple times. just like `ldarg`, there are a few specialized variants of `stloc` and `ldloc`:
- for the first, second, third, and fourth local, you can use `ldloc.0`/`stloc.0`, `ldloc.1`/`stloc.1`, `ldloc.2`/`stloc.2`, and `ldloc.3`/`stloc.3` (without any operands), respectively.
- for any locals with an index less than 256, you can use `ldloc.s`/`stloc.s`, with the index as the operand.
- otherwise you can use the regular `ldloc`/`stloc`, also with the index as the operand.

let's go back to our `WorldMessage` method. if we REALLY want to make sure that we only have one `Console.WriteLine`, we can rewrite the code so that it does this:
```
0 | ldarg.0   |
1 | brfalse.s | 5
2 | ldstr     | "Hello, world"
3 | stloc.0   |
4 | br.s      | 7
5 | ldstr     | "Goodbye, world"
6 | stloc.0   |
7 | ldloc.0   |
8 | call      | Console.WriteLine
9 | ret       |
```
this is basically equivalent to this c# code:
```csharp
public static void WorldMessage(bool hello)
{
    string msg;

    if (hello)
    {
        msg = "Hello, world";
    }
    else
    {
        msg = "Goodbye, world";
    }

    Console.WriteLine(msg);
}
```
### fields
before we write a transpiler, the last thing we need to go over is storing values field and loading the value from them. if you don't know what a field is, or the difference between a field and a property, read below.
<details>
<summary>fields</summary>
fields are essentially just how classes and structs store data. the most basic definition of a field looks something like this:

```csharp
public class Message 
{
    public string text = "hello";
    public string color;
}
```
    
properties are essentially methods of a class that can be called using the same syntax as fields (`x.y` and `x.y = z`). they're often used to represent data that is dynamically calculated or to offer more control over who can set/get a value. for example, here is an example of using a property (`FormattedText` is the property):

```csharp
public class Message
{
    private string text = "hello";
    private string color;

    public string FormattedText 
    {
        get
        {
            return $"<color={color}>{text}</color>";
        }
    }
}
```
the `get` here represents an accessor method. this basically declares a hidden method that runs the code within the get accesor and returns that value. whenever we call `message.FormattedText`. there can also be a set accesor that has a special `value` keyword representing the new value (the one on the right-hand side of `x.y = z`), looking something like this :

```csharp
public class Message
{
    private string text = "hello";
    private string color;

    public string Text
    {
        get     
        {
            return text;
        }
        private set
        {
            text = value;
        }
    }
}
```

there's a variety of ways that properties can be declared. `string Property { get; private set; }` = "example"` generates a hidden field with two accesors, one of which (the set accessor) is private. you can also use `=>`, generating a get accesor; `int Property => example + 5` is equivalent to `int Property { get { return example + 5; } }`. finally, we can also use the `=>` shorthand for property accessor, so `int Property { get => example + 5; }` is also equivalent to that.

while the difference between properties and fields might seem minor, for the purposes of il, they work very differently. properties are accessed through `call`/`callvirt`, whereas we have to use special opcodes for fields. additionally, properties can be overriden, whereas fields cannot.
</details>

storing/loading works like the following. note that all four of these opcodes take in the field as the operand.
- to load from a non-static field, we use `ldfld`, which pops an instance of a class/struct from the stack and then loads that class/struct's value of the field onto the stack
- to store to a non-static field, we use `stfld`, which pops an instance of a class/struct and then a value to store to that class/struct's field
- to load from a static field, we use `ldsfld`, which just loads the field's value onto the stack
- to store to a static field, we use `stfld`, which pops the value and stores it into the field
[^1]: this is kind of incorrect: there are a few opcodes that are also valid as being the last instruction, notably `throw` to throw an error.


