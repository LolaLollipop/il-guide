# il-guide

## first steps

### introduction to IL and transpilers
as you might know, c# (like other .net languages) is compiled. this means that the code you write isn't converted directly to machine code (instructions to the hardware and CPU). instead, c# is compiled into something called IL (intermediate language), which is then read as machine code. 

the problem is that since the code is compiled into IL and no longer is c#, you can't just insert c# code into it. you need a way to insert new IL (or remove/modify/replace old IL). this is where a **harmony transpiler** comes in. a transpiler receives the old instructions and gives new ones, with all of your modifications in it. transpilers are how many exiled events and many plugins work, as they give significantly more control than prefixes and postfixes, and have a much lower chance of breaking other plugins. 

however, in order to write our first transpiler, we first must understand the basics of il.

### IL basics
structually, il is similar to most other programming languages. all of the instructions are in a list, and are read top to bottom. 

for example, the following c# code:
![Csharp](https://github.com/Ruemena/il-guide/assets/135553058/10e965fb-41c4-4ea8-851e-ac33b399e9eb)

would be this in IL:
![IL](https://github.com/Ruemena/il-guide/assets/135553058/a436060d-9be2-4f73-922d-10a2e48b70ae)

you can probably connect a bit of the c# code to the il code, especially with the methods. 

below you can see an example of an instruction. each instruction in IL has an index (green), an offset (blue), an opcode (red), and sometimes an operand (purple).

![instruction](https://github.com/Ruemena/il-guide/assets/135553058/a6f936c2-8b9e-4563-bc8b-4f645bf86295)



the index is where in the list of instructions this specific instruction is located. the offset acts as a reference to the specific instruction (this will become important later).  the **opcode** is probably the most important part. this says what specific instruction will be happening. for example, the opcode to say to add two numbers is `add`. often, you'll need to pass parameters to the opcode. for example, if you wanted to call a method using the `call` opcode, you would need to pass a reference to a method. this is done through the operand - somewhat analogous to providing arguments to a method call. 

