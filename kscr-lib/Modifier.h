#pragma once

enum Modifier : int
{
	Public = 0x0001,
	Protected = 0x0002,
	Internal = 0x0004,
	Private = 0x0008,

	Static = 0x0010,

	Class = 0x00F0,
	Method = 0x0100,
	FieldGetter = 0x0200,
	FieldSetter = 0x0400,
	Constructor = 0x0800,
	Destructor = 0x0F00
};
