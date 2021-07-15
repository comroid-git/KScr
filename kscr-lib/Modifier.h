#pragma once

enum class Modifier
{
	Public = 0x0001,
	Protected = 0x0002,
	Internal = 0x0004,
	Private = 0x0008,

	Static = 0x0010,
	Abstract = 0x0020,
	Final = 0x0040,
	
	Method = 0x0100,
	FieldGetter = 0x0200,
	FieldSetter = 0x0400,
	Constructor = 0x0800,
	Destructor = 0x0F00,

	Class = 0x1000,
	Enum = 0x2000,
	Interface = 0x4000,
	Annotation = 0x8000
};
