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
## our first transpiler
### background info
while there are many opcodes with varying levels of importance (some you'll almost never encounter), 4 big ones are going to be discussed. 

the first one is `ldarg`, which actually encompasses a number of very similar opcodes. if in a *static* method, ldarg pushes an argument of the method onto the stack, based on the zero based index. for arguments 1-4, you have the opcodes Ldarg_0, Ldarg_1, Ldarg_2, and Ldarg_3. however, if you need to access an argument beyond the fourth one, you need to do the `Ldarg_S` opcode, and pass the index as the operand. so, if you're inside `static void Example(int number, float anotherNumber, string notANumber, string anotherNotNumber, List<object> definitelyNotANumber)`, `ldarg_1` would load the float anotherNumber onto the stack, and `ldarg_S 4` loads the list definitelyNotANumber onto the stack (4 being the operand). 

things are slightly different for instance methods. an instance method is any method called on an instance of a class - so, for example, Player.Kill is an instance method. in instance methods, the first argument is the current instance, equivalent to `this`. therefore, `ldarg_0` loads the current instance onto the stack. accessing arguments is done through the usual way, except that you need to simply increment the index of the argument by one. so, if you're inside the instance method `void InstanceExample(int newNumber, string newNotNumber)` which is itself inside the class `ExampleClass`, `ldarg_0` loads the current instance of `ExampleClass` and `ldarg_1` loads the int newNumber onto the stack.

the first of these actually comes as a pair - `call` and `callvirt`. both of these accomplish something similar - a method call.
