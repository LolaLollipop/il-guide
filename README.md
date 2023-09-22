# il-guide

## first steps

### introduction to IL and transpilers
as you might know, c# (like other .net languages) is compiled. this means that the code you write isn't converted directly to machine code (instructions to the hardware and CPU). instead, c# is compiled into something called IL (intermediate language), which is then read as machine code. 

the problem is that since the code is compiled into IL and no longer is c#, you can't just insert c# code into it. you need a way to insert new IL (or remove/modify/replace old IL). this is where a **harmony transpiler** comes in. a transpiler receives the old instructions and gives new ones, with all of your modifications in it. transpilers are how many exiled events and many plugins work, as they give significantly more control than prefixes and postfixes, and have a much lower chance of breaking other plugins. 

however, in order to write our first transpiler, we first must understand the basics of il.

### IL basics
structually, il is similar to most other programming languages. all of the instructions are in a list, and are read top to bottom. 
