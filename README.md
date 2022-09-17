# KScr.NET ![TeamCity Build Status](https://teamcity.comroid.org/app/rest/builds/buildType:(id:project_KScr_Test),branch:master/statusIcon)
A simple interpreter that attempts to grow into an object oriented language (eventually)

## To-Do List

### Statements:
- - [x] `if` and `else`
- - [ ] `mark` and `jump`
- - [x] `try` and `catch`
- - - [x] `throw`
- - - [x] `finally`
- - [x] `do` and `while`
- - [x] `for`
- - [x] ~~`forn`~~ Replaced by foreach because Range implements Iterable
- - [x] `foreach`
- - [ ] `switch` and `case`
- - - [ ] `break` and `continue`

### Language Features:
- - [x] Objects
- - [x] Arrays
- - [ ] Tuple Literals
- - [x] Computed Properties
- - [x] Properties with Getters and Setters
- - [x] Auto-Properties
- - [x] `extends` and `implements`
- - [ ] Variable Caching
- - [ ] Annotations
- - [x] Methods
- - [x] `native` Keyword
- - [x] Static and Dynamic components
- - [x] Prettier StackTrace
- - [x] Compiling into Bytecode
- - [x] Pipe operators (partially done)
- - [ ] Pipe listeners
- - [ ] Reflection
- - [ ] Encoding Support
- - [ ] LLVM Support

### Working Code Files:
- - [x] `HelloWorld.kscr`
- - [x] `PrintNumbers.kscr` (current sandbox)
- - [x] `MathFromIO.kscr`
- - [x] `ToStringTest.kscr` (requires objects)
- - [ ] `FileIO.kscr` (requires File core class)
- - [ ] `Function.kscr`
- - [ ] Core Module

### Other To-Do Items:
- - [x] Automate TokenType scanning (done by ANTLR)
- - [ ] IDEA Language Support
- - [ ] VSC Language Support
- - [ ] Module Packaging
- - [ ] Something for Databinding
- - [ ] More system Classes
