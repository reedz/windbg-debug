#![allow(dead_code)]
#![allow(unused_variables)]
#![allow(unused_assignments)]

use std::cell::Cell;
use std::cell::RefCell;
use std::rc::Rc;
use std::sync::Arc;

pub struct UsualStructure {
    integer: i32,
    stringo: String,
}

#[derive(Clone, Copy)]
struct GenericStructure<T>(T);

struct AutoStructure(i32, u64);
struct EmptyStructure;

pub fn generic_method<T>(x: T) -> T { x }

enum SimpleEnum {
    A,
    B(i32)
}

enum ComplexEnum {
    A(GenericStructure<i64>),
    B{integer: i32, stringo: String},
}

// This triggers the non-zero optimization that yields a different
// enum representation in the debug info.
// taken from GDB test suite
enum SpaceSaver {
    Thebox(u8, Box<i32>),
    Nothing,
}

fn func(arg: String) {
    println!("{}", arg);
}

fn main () {
    let nothing = ();
    let uint1: u8 = 1;
    let uint2: u16 = 2;
    let uint3: u32 = 3;
    let uint4: u64 = 4;

    let int1: i8 = 1;
    let int2: i16 = 2;
    let int3: i32 = 3;
    let int4: i64 = 4;

    let float = 5.5;

    let str1 = "sample text with spaces!";
    let str2 = "lazy brown fox".to_string();
    let str_binary = b"achtung";

    let option_some: Option<i32> = Option::Some(1);
    let option_none: Option<i32> = Option::None;

    let result_value: Result<i32, &str> = Result::Ok(8);
    let result_error: Result<i32, &str> = Result::Err("Errawr");

    let slice_empty: [i32;0] = [];
    let slice_repeated = [5; 10];
    let slice_with_strings = ["a", "b", "c", "d"];
    let slice_part = &slice_with_strings[0..3];

    let tuple = (42, "zelda", 7);

    let space_saver_nothing = SpaceSaver::Nothing;
    let space_saver_box = SpaceSaver::Thebox(17, Box::new(1729));

    let vector_ints = vec![1,2,3];
    let vector_strings = vec!["a", "b", "c"];

    let enum_simple_empty = SimpleEnum::A;
    let enum_simple_filled = SimpleEnum::B(5);

    let enum_complex_a = ComplexEnum::A(GenericStructure(5));
    let enum_complex_b = ComplexEnum::B {
        integer: 5, 
        stringo: "stringo".to_string()
    };

    let structure_empty_structure = EmptyStructure;
    let structure_auto = AutoStructure(9, 17);
    let structure_usual = UsualStructure {
        integer: 5,
        stringo: "well".to_string()
    };
    let structure_generic = GenericStructure::<i16>(4);
    let method_generic_result = generic_method(5);

    let box_int = Box::new(4);
    let cell_int = Cell::new(5);
    let cell_ref_int = RefCell::new(6);
    let rc = Rc::new(5);
    let arc = Arc::new(7);

    func("test".to_string());   

    println!("That's all, folks!");
}