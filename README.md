# KScr.NET ![TeamCity Build Status](https://teamcity.comroid.org/app/rest/builds/buildType:(id:project_KScr_Test),branch:master/statusIcon)
A simple interpreter that attempts to grow into an object oriented language (eventually)

## To-Do List

### Statements:
- - [x] `if` and `else`
- - [ ] `mark` and `jump`
- - [ ] `try` and `catch`
- - - [x] `throw`
- - - [x] `finally`
- - [x] `do` and `while`
- - [x] `for`
- - [ ] ~~`forn`~~ Replaced by foreach because Range implements Iterable
- - [x] `foreach`
- - [ ] `switch` and `case`
- - - [ ] `break` and `continue`

### Language Features:
- - [x] Objects
- - [ ] Arrays
- - [ ] Tuple Literals
- - [x] Computed Properties
- - [ ] Properties with Getters and Setters
- - [x] Auto-Properties
- - [x] `extends` and `implements`
- - [ ] Variable Caching
- - [ ] Annotations
- - [x] Methods
- - [ ] `native` Keyword
- - [x] Static and Dynamic components
- - [x] Prettier StackTrace
- - [x] Compiling into Bytecode
- - [ ] Pipe operators
- - [ ] Reflection
- - [ ] Encoding Support
- - [ ] LLVM Support

### Working Code Files:
- - [x] `HelloWorld.kscr`
- - [x] `PrintNumbers.kscr`
- - [x] `MathFromIO.kscr`
- - [ ] `ToStringTest.kscr` (requires objects)
- - [ ] `File.kscr` (core class; requires native class members)
- - [ ] `FileIO.kscr` (requires File core class)
- - [ ] `Function.kscr`
- - [ ] Core Module

### Other To-Do Items:
- - [ ] Automate TokenType scanning
- - [ ] IDEA Language Support
- - [ ] VSC Language Support
- - [ ] Packaging
- - [ ] Something for Databinding
- - [ ] More system Classes
