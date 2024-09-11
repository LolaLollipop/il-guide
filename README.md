# il-guide
this is v2 of a guide on CIL and how to write transpilers. this is meant to be very gentle, and many of the more technical or abstracted concepts will be explained. however, i highly recommend that you have a good grasp on c# and harmony (prefixes and postfixes).

other resources:
- [sharplab](https://sharplab.io/): this can convert c# to il in real time, which is extremely helpful for understanding how something may be compiled into il.
- [microsoft's list of opcodes](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes?view=net-8.0) every opcode, and their expected operand, can be found here.
- cil [partition 1](https://download.microsoft.com/download/7/3/3/733ad403-90b2-4064-a81e-01035a7fe13c/ms%20partition%20i.pdf) [partition 2](https://download.microsoft.com/download/7/3/3/733ad403-90b2-4064-a81e-01035a7fe13c/ms%20partition%20ii.pdf) [partition 3](https://download.microsoft.com/download/7/3/3/733ad403-90b2-4064-a81e-01035a7fe13c/ms%20partition%20iii.pdf): these are official microsoft documents on the implementation of CIL. while these are very technical, they can be helpful once you have a good grasp on il.


## introduction to CIL
### what even is CIL?
you're probably familiar with compiling C# code. when you press build on a plugin, it generates a dll containing bytecode, which is a non-human readable format that will later be read and executed. this format is called common intermediate language (CIL, though often shorted to just IL), and when your plugin is loaded, it (usually) converts this into machine code, or instructions that your hardware knows how to handle. that's where the "intermediate" part comes from: its between a language like C# and machine code.

### the virtual execution system
