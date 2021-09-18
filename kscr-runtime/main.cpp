#include <functional>
#include <iostream>
#include <string>
#include <vector>
#include "../kscr-lib/NameCache.h"
#include "../kscr-eval/Eval.h"
#include "../kscr-eval/Token.h"
#include "../kscr-eval/BytecodePacket.h"

void compile()
{
	//Tokenizer::tokenize(&Const::files);
}

int run()
{
	//return Const::typeCache.runEntryPoint();
	return 0;
}

// eval test
auto main(int argc, char* argv[]) -> int
{
	auto cache = NameCache();
	
	cache["hello"]->id = 123;
	auto check = cache["hello"]->id == 123;
	std::cout << "First set was " << check;

	cache["helloasfdgjehgi2ouw34rtnsoiajdvgnpqewioruthgsdifuvbnsaieufrhcasoiudghasliucghaslkjgfhasdlkjfghadslkjfghalksjdfhoeacisurthsdr"]->id = 456;
	check = cache["helloasfdgjehgi2ouw34rtnsoiajdvgnpqewioruthgsdifuvbnsaieufrhcasoiudghasliucghaslkjgfhasdlkjfghadslkjfghalksjdfhoeacisurthsdr"]->id == 456;
	std::cout << "Second set was " << check;


	//try{
		// debug code statement
		const std::string code = "num aNum = 1; return aNum + 1;";

		//// parse code into tokens
		const std::vector<Token> tokens = Eval::tokenize(code.data(), static_cast<int>(code.length()));

		// compile tokens into bytecode
		const Bytecode* bytecode = Eval::compile(&tokens);

		// run bytecode
		return Eval::execute(bytecode);
	/*}
	catch (std::exception& e)
	{
		std::cerr
		<< "An internal exception occurred:" << std::endl
		<< "\t- " << std::string(e.what()) << std::endl;
		return -1;
	}
	catch (const int i) {
		std::cerr << "An exit code was thrown: " << i << std::endl;
		return i;
	}
	catch (const long l) {
		std::cerr << "An exit code was thrown: " << l << std::endl;
		return l;
	}
	catch (const char* p) {
		std::cerr
		<< "An internal message was thrown:" << std::endl
		<< "\t- " << std::string(p) << std::endl;
		return -1;
	}
	catch (...) {
		std::cout << "nope, sorry, I really have no clue what that is\n";
	}*/
}

/* // lib test
auto main(int argc, char* argv[]) -> int
{
	if (argc == 0)
	{
		std::cout << "Missing source";
		return -1;
	}
	for (int i = 0; i < argc; ++i)
	{
		std::string str(argv[i]);

		// take input files
		if (str.find(".kscr") && Filesystem::exists(str))
			Const::files.push_back(str);
	}
	
	tokenize();
	const int exitCode = run();
	std::cout << "Program finished with exit code " << exitCode << std::endl;
	return exitCode;
}
*/
