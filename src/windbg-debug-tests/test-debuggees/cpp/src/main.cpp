#include "stdafx.h"
#include <tuple>
#include <map>
#include <memory>
#include <string>
#include <vector>

std::tuple<int, bool, float> foo()
{
	return std::make_tuple(128, true, 1.5f);
}
int main()
{
	std::tuple<int, bool, float> result = foo();
	int value = std::get<0>(result);
	int obj1;
	bool obj2;
	float obj3;
	std::tie(obj1, obj2, obj3) = foo();

	std::vector<int> vec = { 1, 2, 3, 4, 5 };
	std::map<std::string, int> map = { { "Foo", 10 },{ "Bar", 20 } };
	std::string str = "Some text";
	std::unique_ptr<int> ptr1 = std::make_unique<int>(8);
	std::shared_ptr<int> ptr2 = std::make_shared<int>(16);
	int integer_array[3] = { 5, 10, 15 };

	std::string &str_reference = str;
	std::string *str_pointer = &str;

	std::string **pointer_pointer = &str_pointer;

	printf("set a breakpoint here");

	return 0;
}

