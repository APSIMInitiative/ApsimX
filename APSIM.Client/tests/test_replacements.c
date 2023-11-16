#include <check.h>
#include "replacement.h"

START_TEST(test_create_int_replacement) {
    char* path = "some path";
    replacement_t* repl = createIntReplacement(path, 709120722);
    ck_assert_int_eq(0, repl->paramType);
    ck_assert_str_eq(path, repl->path);
    ck_assert_int_eq(4, repl->value_len);
    ck_assert_uint_eq((unsigned char)0xd2, (unsigned char)repl->value[0]);
    ck_assert_uint_eq((unsigned char)0x52, (unsigned char)repl->value[1]);
    ck_assert_uint_eq((unsigned char)0x44, (unsigned char)repl->value[2]);
    ck_assert_uint_eq((unsigned char)0x2a, (unsigned char)repl->value[3]);
    replacement_free(repl);

    path = "";
    repl = createIntReplacement(path, -1948869934);
    ck_assert_int_eq(0, repl->paramType);
    ck_assert_str_eq(path, repl->path);
    ck_assert_int_eq(4, repl->value_len);
    ck_assert_uint_eq((unsigned char)0xd2, (unsigned char)repl->value[0]);
    ck_assert_uint_eq((unsigned char)0x9a, (unsigned char)repl->value[1]);
    ck_assert_uint_eq((unsigned char)0xd6, (unsigned char)repl->value[2]);
    ck_assert_uint_eq((unsigned char)0x8b, (unsigned char)repl->value[3]);
    replacement_free(repl);
}
END_TEST

START_TEST(test_create_double_replacement) {
    char* path = "xyz";
    double val = -1985691576917547.5;
    replacement_t* repl = createDoubleReplacement(path, val);
    ck_assert_int_eq(1, repl->paramType);
    ck_assert_str_eq(path, repl->path);
    ck_assert_int_eq(8, repl->value_len);
    ck_assert_uint_eq((unsigned char)0xae, (unsigned char)repl->value[0]);
    ck_assert_uint_eq((unsigned char)0x78, (unsigned char)repl->value[1]);
    ck_assert_uint_eq((unsigned char)0x4d, (unsigned char)repl->value[2]);
    ck_assert_uint_eq((unsigned char)0x64, (unsigned char)repl->value[3]);
    ck_assert_uint_eq((unsigned char)0xe7, (unsigned char)repl->value[4]);
    ck_assert_uint_eq((unsigned char)0x37, (unsigned char)repl->value[5]);
    ck_assert_uint_eq((unsigned char)0x1c, (unsigned char)repl->value[6]);
    ck_assert_uint_eq((unsigned char)0xc3, (unsigned char)repl->value[7]);
    replacement_free(repl);

    repl = createDoubleReplacement("", 10);
    ck_assert_int_eq(1, repl->paramType);
    ck_assert_str_eq("", repl->path);
    ck_assert_int_eq(8, repl->value_len);
    ck_assert_uint_eq((unsigned char)0x0, (unsigned char)repl->value[0]);
    ck_assert_uint_eq((unsigned char)0x0, (unsigned char)repl->value[1]);
    ck_assert_uint_eq((unsigned char)0x0, (unsigned char)repl->value[2]);
    ck_assert_uint_eq((unsigned char)0x0, (unsigned char)repl->value[3]);
    ck_assert_uint_eq((unsigned char)0x0, (unsigned char)repl->value[4]);
    ck_assert_uint_eq((unsigned char)0x0, (unsigned char)repl->value[5]);
    ck_assert_uint_eq((unsigned char)0x24, (unsigned char)repl->value[6]);
    ck_assert_uint_eq((unsigned char)0x40, (unsigned char)repl->value[7]);
    replacement_free(repl);
}
END_TEST

START_TEST(test_create_double_array_replacement) {
    char* path = "x";
    int length = 3;
    double values[3] = { 0.75, 0.25, -0.25 };

    replacement_t* repl = createDoubleArrayReplacement(path, values, length);
    ck_assert_int_eq(6, repl->paramType);
    ck_assert_str_eq(path, repl->path);
    ck_assert_int_eq(24, repl->value_len);
    unsigned char expected[24] = {
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0xe8,
        0x3f,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0xd0,
        0x3f,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0xd0,
        0xbf,
    };
    for (int i = 0; i < 24; i++) {
        unsigned char exp = expected[i];
        unsigned char act = repl->value[i];
        ck_assert_uint_eq(exp, act);
    }

    replacement_free(repl);
}
END_TEST

Suite* replacements_test_suite() {
    Suite* suite;
    TCase* test_case;

    suite = suite_create("Replacements Tests");
    test_case = tcase_create("core case");

    tcase_add_test(test_case, test_create_int_replacement);
    tcase_add_test(test_case, test_create_double_replacement);
    tcase_add_test(test_case, test_create_double_array_replacement);

    suite_add_tcase(suite, test_case);
    return suite;
}
