parser grammar KScrParser;

options { tokenVocab = KScrLexer; }

file: packageDecl imports classDecl* EOF;

// file-related
packageDecl: PACKAGE id SEMICOLON;

imports: importDecl*;
importDecl: IMPORT STATIC? id (DOT STAR)? SEMICOLON;

// class-related
annotationArg: idPart ASSIGN expr;
annotation: AT id (LPAREN (annotationArg (COMMA annotationArg)* | expr)? RPAREN)?;

modifier
    : PUBLIC        #modPublic
    | INTERNAL      #modInternal
    | PROTECTED     #modProtected
    | PRIVATE       #modPrivate
    | STATIC        #modStatic
    | FINAL         #modFinal
    | ABSTRACT      #modAbstract
    | NATIVE        #modNative
    | SERVE         #modServe
    | SYNCHRONIZED  #modSyncronized
    ;
modifiers: modifier+;

classType
    : CLASS         #ctClass
    | INTERFACE     #ctInterface
    | ENUM          #ctEnum
    | ANNOTATION    #ctAnnotation
    ;

genericTypeUses: LESSER (n=NUMLIT | first=type) (COMMA type)* GREATER;

num: NUMIDENT           #numTypeLit
    | BOOL              #numTypeLitBool
    | BYTE              #numTypeLitByte
    | SHORT             #numTypeLitShort
    | INT               #numTypeLitInt
    | LONG              #numTypeLitLong
    | FLOAT             #numTypeLitFloat
    | DOUBLE            #numTypeLitDouble
    ;
primitiveTypeLit 
    : OBJECT        #typeLitObject
    | ARRAYIDENT    #typeLitArray
    | TUPLEIDENT    #typeLitTuple
    | num           #typeLitNum
    | TYPE          #typeLitType
    | ENUM          #typeLitEnum
    | VOID          #typeLitVoid
    ;
primitiveLit
    : THIS          #varThis
    | SUPER         #varSuper
    | NUMLIT        #varLitNum
    | TRUE          #varLitTrue
    | FALSE         #varLitFalse
    | STRLIT        #varLitStr
    | STDIOLIT      #varLitStdio
    | ENDLLIT       #varLitEndl
    | NULL          #varLitNull
    ;
rawType
    : primitiveTypeLit
    | idPart 
    | id
    ;
type: rawType genericTypeUses? nullable=QUESTION? (indexerEmpty | ELIPSES)? nullableArray=QUESTION?;

genericTypeDef: idPart elp=ELIPSES? (EXTENDS ext=type | SUPER sup=type)? (ASSIGN (defN=NUMLIT | def=type))?;
genericDefs: LESSER (NUMLIT | genericTypeDef) (COMMA genericTypeDef)* GREATER;

parameter: FINAL? type idPart (ASSIGN expr)?;
parameters: LPAREN (parameter (COMMA parameter)*)? RPAREN;
arguments: LPAREN (expr (COMMA expr)*)? RPAREN;

statements: statement*;
noBlock: SEMICOLON;
uniformBlock: expr | statement;
normalBlock: LBRACE statements RBRACE;
memberExpr: REQARROW uniformBlock;
lambdaBlock: RDASHARROW (uniformBlock | normalBlock);
caseBlock
    : COLON statements BREAK SEMICOLON  #caseStmtBlock
    | REQARROW memberExpr COMMA                  #caseExprBlock
    ;
memberBlock
    : normalBlock               #memberNormalBlock
    | memberExpr SEMICOLON      #memberExprBlock
    | noBlock                   #memberNoBlock
    ;
codeBlock
    : normalBlock               #codeNormalBlock
    | statement                 #codeStmtBlock
    | noBlock                   #codeNoBlock
    ;

initDecl: STATIC memberBlock;
subConstructorCall: (THIS | type) arguments;
subConstructorCalls: COLON subConstructorCall (COMMA subConstructorCall)*?;
constructorDecl: annotation* modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? 
        type parameters subConstructorCalls? memberBlock;
methodDecl: annotation* modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? 
        genericDefs? type idPart parameters genericSpecifiers? memberBlock;
indexerMemberDecl: annotation* modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? 
        genericDefs? type THIS indexerDecl genericSpecifiers? propBlock;

propGetter: GET modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? memberBlock;
propSetter: SET modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? memberBlock;
propInit: INIT modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? memberBlock;
propBlock
    : memberBlock                                       #propComputed
    | LBRACE propGetter propSetter? propInit? RBRACE    #propAccessors
    | (ASSIGN expr)? SEMICOLON                          #propFieldStyle
    ;
propertyDecl: annotation* modifiers? (WHERE condition=expr)? (SELECT supplier=lambda)? (ELSE (NULL | fallback=lambda))? 
        type idPart propBlock;

member
    : propertyDecl                                          #memProp
    | classDecl                                             #memCls
    | initDecl catchBlocks?                                 #memInit
    | constructorDecl catchBlocks?                          #memCtor
    | methodDecl catchBlocks?                               #memMtd
    | indexerMemberDecl                                     #memIdx
    | idPart arguments? ASSIGN expr (COMMA | SEMICOLON?)    #memEnumConst
    ;
genericSpecifier: idPart (superclassesDef | (ASSIGN (type | expr)) | superclassesDef (ASSIGN (type | expr)));
genericSpecifiers: WHERE genericSpecifier (COMMA genericSpecifier)*;
superclassesDef: COLON type (COMMA type)*;

classDecl: annotation* modifiers? classType idPart genericDefs? superclassesDef? genericSpecifiers? (LBRACE member* RBRACE | SEMICOLON);

inferType: VOID | VAR;

indexerEmpty: LSQUAR COMMA* RSQUAR;
indexerDecl: (LSQUAR) type idPart (COMMA type idPart)* (RSQUAR);
indexerExpr: (LSQUAR) expr (COMMA expr)* (RSQUAR);
cast: LPAREN type COLON expr RPAREN;
declaration: type idPart (ASSIGN expr)?;
mutation: (binaryop | binaryop_late)? ASSIGN expr;
call: idPart arguments;
ctorCall: NEW type arguments;
newArray: NEW type indexerExpr;
newListedArray: NEW type indexerEmpty LBRACE (expr (COMMA expr)*)? RBRACE;
label: idPart COLON WS;
lambda
    : label? (type COLON)? idPart        #methodRef
    | label? tupleExpr lambdaBlock    #lambdaExpr 
    ;

returnStatement: YIELD? RETURN expr?;
throwStatement: THROW expr;

markStatement: MARK idPart SEMICOLON;
jumpStatement: JUMP idPart SEMICOLON;

tryCatchStatement: TRY codeBlock;
tryWithResourcesStatement: TRY LPAREN declaration (COMMA declaration)* RPAREN codeBlock;
catchBlocks: catchBlock* finallyBlock;
catchBlock: CATCH (LPAREN type (COMMA type)* idPart RPAREN)? codeBlock;
finallyBlock: FINALLY codeBlock;

ifStatement: IF LPAREN expr RPAREN codeBlock elseStatement?;
elseStatement: ELSE codeBlock;

whileStatement: WHILE LPAREN expr RPAREN codeBlock;
forStatement: FOR LPAREN init=statement cond=expr SEMICOLON acc=expr RPAREN codeBlock;
foreachStatement: FOREACH LPAREN idPart COLON expr RPAREN codeBlock;
doWhile: DO codeBlock WHILE LPAREN expr RPAREN SEMICOLON;

switchStatement: SWITCH tupleExpr LBRACE caseClause* defaultClause? RBRACE;
caseClause: CASE tupleExpr caseBlock;
defaultClause: DEFAULT caseBlock;

statement
    : declaration SEMICOLON                                 #stmtDeclare
    | left=expr mutation SEMICOLON                          #stmtAssign
    | left=tupleExpr ASSIGN right=tupleExpr                 #stmtAssignTuple
    | left=expr DOT idPart arguments?                       #stmtCallMember
    | returnStatement SEMICOLON                             #stmtReturn
    | throwStatement SEMICOLON                              #stmtThrow
    | markStatement                                         #stmtMark
    | jumpStatement                                         #stmtJump
    | tryCatchStatement catchBlocks?                        #stmtTryCatch
    | tryWithResourcesStatement catchBlocks?                #stmtTryWithRes
    | ifStatement catchBlocks?                              #stmtIf
    | whileStatement catchBlocks?                           #stmtWhile
    | doWhile catchBlocks?                                  #stmtDoWhile
    | forStatement catchBlocks?                             #stmtFor
    | foreachStatement catchBlocks?                         #stmtForeach
    | switchStatement catchBlocks?                          #stmtSwitch
    | pipe=expr (RREQARROW lambda)+ SEMICOLON               #stmtPipeListen
    | pipe=expr (RRDASHARROW expr)+ SEMICOLON               #stmtPipeRead
    | pipe=expr (LLDASHARROW expr)+ SEMICOLON               #stmtPipeWrite
    | SEMICOLON                                             #stmtEmpty
    ;
typedExpr: type? expr;
expr
    // operators
    : prefixop expr                                         #opPrefix
    | left=expr binaryop right=expr                         #opBinary
    | expr postfixop                                        #opPostfix
    // `is` keyword
    | expr IS type idPart?                                  #checkInstanceof
    // syntax components
    | YIELD expr                                            #yieldExpr
    | target=expr indexerExpr                               #readIndexer
    | LPAREN expr RPAREN                                    #parens
    | cond=expr QUESTION left=expr COLON right=expr         #ternary
    | cast                                                  #exprCast
    // simply a variable
    | idPart                                                #varValue
    | newArray                                              #newArrayValue
    | newListedArray                                        #newListedArrayValue
    | primitiveLit                                          #nativeLitValue
    | type                                                  #typeValue
    | lambda                                                #exprLambda
    // variable mutation
    | declaration                                           #varDeclare
    | left=expr mutation                                    #varAssign
    // member calls
    | left=expr DOT idPart arguments?                       #exprCallMember
    | ctorCall                                              #callCtor
    // statement expressions
    | throwStatement                                        #exprThrow
    | switchStatement                                       #exprSwitch
    // range invocator
    | left=expr SHORTELIPSES right=expr                     #rangeInvoc
    // pipe operators
    | pipe=expr (RREQARROW lambda)+                         #exprPipeListen
    | pipe=expr (RRDASHARROW expr)+                         #exprPipeRead
    | pipe=expr (LLDASHARROW expr)+                         #exprPipeWrite
    // tuple expressions
    | tupleExpr                                             #exprTuple
    | left=expr binaryop_late right=expr                    #opBinaryLate
    ;

tupleDeclType: type idPart?;
tupleDecl: LPAREN tupleDeclType (COMMA tupleDeclType)* RPAREN;
tupleExpr: LPAREN typedExpr (COMMA typedExpr)* RPAREN;

binaryop
    : STAR                  #opMultiply
    | SLASH                 #opDivide
    | PERCENT               #opModulus
    | BITAND                #opBitAnd
    | BITOR                 #opBitOr
    | EXCLAMATION           #opBitNot
    | AND                   #opLogicAnd
    | OR                    #opLogicOr
    | UP                    #opPow
    | EQUAL                 #opEqual
    | INEQUAL               #opInequal
    | GREATEREQ             #opGreaterEq
    | LESSEREQ              #opLesserEq
    | GREATER               #opGreater
    | LESSER                #opLesser
    | LSHIFT                #opLShift
    | RSHIFT                #opRShift
    | ULSHIFT               #opULShift
    | URSHIFT               #opURShift
    | QUESTION QUESTION     #opNullFallback
    ;
binaryop_late // binary operators that have less precedence
    : PLUS                  #opPlus
    | MINUS                 #opMinus 
    ;

prefixop
    : MINUS         #opArithNot
    | EXCLAMATION   #opLogicNot
    | PLUSPLUS      #opIncrRead
    | MINUSMINUS    #opDecrRead
    ;

postfixop
    : PLUSPLUS      #opReadIncr
    | MINUSMINUS    #opReadDecr
    ;

id: idPart (DOT idPart)*;
// contextual keywords are valid identifiers
idPart
    : ID
    | ANNOTATION
    ;
