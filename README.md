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
