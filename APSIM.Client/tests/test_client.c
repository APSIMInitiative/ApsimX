#include <assert.h>
#include <check.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <pthread.h>

#include "replacement.h"
#include "apsimclient.h"
#include "client-private.h"
#include "protocol.h"

//tmp
#include <stdlib.h>
#include <errno.h>
#include <stdio.h>
//endtmp
int server_socket, client_socket, connection_socket;
char* pipe_file;

void setup() {
    server_socket = -1;
    client_socket = -1;
    connection_socket = -1;
    pipe_file = "";
}

// The teardown method closes any open FDs and removes the pipe file
// from the filesystem. This MUST be run after all tests, but it is not
// called automatically if a test fails, so it must be called manually
// before a failed assertion occurs.
void teardown() {
    if (connection_socket != -1) {
        int err = close(connection_socket);
        if (err != 0) {
            printf("Error closing connection socket: %s\n", strerror(errno));
        }
        // connection_socket = -1;
    }
    if (client_socket != -1) {
        int err = close(client_socket);
        if (err != 0) {
            printf("Error closing client socket (%d): %s\n", errno, strerror(errno));
        }
        // client_socket = -1;
    }
    if (server_socket != -1) {
        int err = close(server_socket);
        if (err != 0) {
            printf("Error closing server socket (%d): %s\n", errno, strerror(errno));
        }
        // server_socket = -1;
    }
    if (strcmp(pipe_file, "") != 0 && access(pipe_file, F_OK) == 0) {
        remove(pipe_file);
    }
}

// Create a server on localhost, listening on the given port,
// then create a client and connect to the server.
void establish_remote_connection(uint16_t port) {
    // Create a server and listen for connections.
    server_socket = socket(AF_INET, SOCK_STREAM, 0);
    struct sockaddr_in address;
    address.sin_family = AF_INET;
    address.sin_port = htons(port);
    // (0x7f000001 = 127.0.0.1)
    address.sin_addr.s_addr = inet_addr("127.0.0.1");//htonl(0x7f000001);
    int addr_len = sizeof(address);

    // After being closed, the AF_INET socket will go into a TIME_WAIT
    // state for a period of time, during which further calls to bind()
    // will fail. This will cause the unit tests to fail if run
    // multiple times in quick succession. To work around this issue,
    // we enable the SO_REUSEADDR socket option, to allow the address
    // to be reused even when it's in a TIME_WAIT state.
    int opt = 1;
    setsockopt(server_socket, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(int));

    int err = bind(server_socket, (struct sockaddr*)&address, addr_len);
    if (err < 0) {
        printf("bind() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }

    err = listen(server_socket, 10);
    if (err < 0) {
        printf("listen() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }

    // Use the apsim client API to connect to the server.
    client_socket = connectToRemoteServer("127.0.0.1", port);

    // Accept the incoming connection from the client.
    connection_socket = accept(server_socket, (struct sockaddr*)&address, (socklen_t*)&addr_len);
    if (connection_socket < 0) {
        printf("accept() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_ge(connection_socket, 0);
    }
}

void establish_connection() {
    // Create a server and listen for connections.
    server_socket = socket(AF_UNIX, SOCK_STREAM, 0);
    struct sockaddr_un address;
    address.sun_family = AF_UNIX;
    pipe_file = "/tmp/CoreFxPipe_apsimclient-unittest";
    strcpy(address.sun_path, pipe_file);
    int addr_len = sizeof(address);

    int err = bind(server_socket, (struct sockaddr*)&address, addr_len);
    if (err < 0) {
        fprintf(stderr, "bind() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }

    err = listen(server_socket, 10);
    if (err < 0) {
        fprintf(stderr, "listen() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }

    // Use the apsim client API to connect to the server.
    client_socket = connectToServer("apsimclient-unittest");

    // Accept the incoming connection from the client.
    connection_socket = accept(server_socket, (struct sockaddr*)&address, (socklen_t*)&addr_len);
    if (connection_socket < 0) {
        fprintf(stderr, "accept() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_ge(connection_socket, 0);
    }
}

// Read a message from the client, using the expected protocol.
/*
 * Read a message from the client, using the expected protocol.
 * @param   in      int*        Message length. (out parameter)
 * @return          char*       The message received from the client (must be freed by the caller).
 */
void* read_from_client_detect_length(void* in) {
    uint32_t* msg_length = (uint32_t*)in;
    char msg_len[4];
    int err = read(connection_socket, msg_len, 4);
    if (err < 0) {
        printf("read() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(4, err);
    }

    uint32_t n;
    memcpy(&n, msg_len, 4);

    unsigned char* buf = malloc((n + 1) * sizeof(unsigned char));
    buf[n] = 0;
    err = read(connection_socket, buf, n);
    if (err < 0) {
        printf("read() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(n, err);
    }
    *msg_length = n;
    return buf;
}

/**
 * Read a message with the given length from the client.
 * @param   in      int*          Message length (in parameter).
 * @return          char*         Message received from the client (must be freed by the caller).
 */
void* read_from_client_with_length(void* in) {
    uint32_t msg_length = *(uint32_t*)in;
    unsigned char* buf = malloc((msg_length + 1) * sizeof(unsigned char));
    buf[msg_length] = 0;
    int err = read(connection_socket, buf, msg_length);
    if (err < 0) {
        printf("read() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(msg_length, err);
    }
    if (err != msg_length) {
        fprintf(stderr, "read() failure: Expected %d bytes but got %d\n", msg_length, err);
        assert(err == msg_length);
    }
    return buf;
}

/**
 * Send a message to the client, using the expected protocol.
 * @param in            char*       the message.
 * @return              NULL        Nothing.
 */
void* send_to_client(void* in) {
    char* message = (char*)in;
    size_t len = strlen(message);
    // Send message length.
    int err = send(connection_socket, (char*)&len, 4, 0);
    if (err < 0) {
        printf("send() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(4, err);
    }

    // Send the message itself.
    err = send(connection_socket, message, len, 0);
    if (err < 0) {
        printf("send() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(len, err);
    }
    return NULL;
}

/*
 * Read a Replacement struct from the client, using the expected
 * protocol.
 * @return      replacemtn_t*       The replacement struct send by the client.
 */
void* read_replacement_from_client() {
    // 1. Read parameter path.
    uint32_t path_len;
    char* path = (char*)read_from_client_detect_length(&path_len);
    send_to_client("ACK");

    // 2. Read parameter type.
    uint32_t length = sizeof(int);
    char* param_type_bytes = (char*)read_from_client_with_length(&length);
    int32_t param_type = 0;
    for (uint32_t i = 0; i < 4; i++) {
        param_type += param_type_bytes[i] << i;
    }
    // memcpy(&param_type, param_type_bytes, 4);
    send_to_client("ACK");
    
    uint32_t param_len;
    if (param_type == 0)
        param_len = sizeof(int32_t);
    else if (param_type == 1)
        param_len = sizeof(double);
    else {
        fprintf(stderr, "tbi: serverside parameter handling for param type %d\n", param_type);
        assert (param_type == 0 || param_type == 1);
    }
    char* param_value = (char*)read_from_client_with_length(&param_len);
    send_to_client("ACK");
    
    replacement_t* replacement = malloc(sizeof(replacement_t));
    replacement->path = path;
    replacement->paramType = param_type;
    replacement->value_len = param_len;
    replacement->value = param_value;
    return replacement;
}

/**
 * Assert that two replacement structs are equal.
 */
void assert_replacements_equal(replacement_t* expected, replacement_t* actual) {
    if (expected->paramType != actual->paramType) {
        teardown();
        ck_assert_int_eq(expected->paramType, actual->paramType);
    }
    if (strcmp(expected->path, actual->path) != 0) {
        teardown();
        ck_assert_str_eq(expected->path, actual->path);
    }
    if (expected->value_len != actual->value_len) {
        teardown();
        ck_assert_int_eq(expected->value_len, actual->value_len);
    }
    for (uint32_t i = 0; i < expected->value_len; i++) {
        if (expected->value[i] != actual->value[i]) {
            teardown();
            ck_assert_int_eq(expected->value[i], actual->value[i]);
        }
    }
}

/**
 * Use the client API to send a Replacement struct to the server, and ensure
 * that the data transmission follows the replacements protocol.
 */
void test_send_replacement(replacement_t* change) {
    pthread_t tid;
    pthread_create(&tid, NULL, read_replacement_from_client, NULL);
    sendReplacementToSocket(client_socket, change);
    replacement_t* from_client;
    pthread_join(tid, (void**)&from_client);

    assert_replacements_equal(change, from_client);
    replacement_free(from_client);
}

/**
 * Read a RUN command from the client.
 * @param in the number of changes to expect.
 * @return the changes sent by the client.
 */
void* read_run_command_from_client(void* in) {
    uint32_t* num_changes = (uint32_t*)in;
    // 1. We expect "RUN" from the client.
    uint32_t msg_len;
    char* msg = (char*)read_from_client_detect_length(&msg_len);
    if (msg_len != 3) {
        teardown();
        ck_assert_int_eq(3, msg_len);
    }
    char* expected = "RUN";
    if (strcmp(expected, msg) != 0) {
        teardown();
        ck_assert_str_eq(expected, msg);
    }
    free(msg);
    send_to_client("ACK");

    // 2. The client will now send through the changes one by one.
    replacement_t** changes = malloc(*num_changes * sizeof(replacement_t*));
    for (uint32_t i = 0; i < *num_changes; i++) {
        changes[i] = read_replacement_from_client();
    }

    // 3. The client will now send through a "FIN".
    msg = (char*)read_from_client_detect_length(&msg_len);
    if (msg_len != 3) {
        teardown();
        ck_assert_int_eq(3, msg_len);
    }
    expected = "FIN";
    if (strcmp(expected, msg) != 0) {
        teardown();
        ck_assert_str_eq(expected, msg);
    }
    free(msg);
    send_to_client("ACK");
    send_to_client("FIN");

    return changes;
}

void run_with_changes(replacement_t** changes, uint32_t nchanges) {
    pthread_t tid;
    replacement_t** changes_from_client;
    int err = pthread_create(&tid, NULL, read_run_command_from_client, &nchanges);
    if (err != 0) {
        printf("pthread_join() error: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(err, 0);
    }
    runWithChanges(client_socket, changes, nchanges);
    err = pthread_join(tid, (void**)&changes_from_client);
    if (err != 0) {
        printf("pthread_join() error: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(err, 0);
    }

    for (uint32_t i = 0; i < nchanges; i++) {
        assert_replacements_equal(changes[i], changes_from_client[i]);
        replacement_free(changes_from_client[i]);
    }
}

void basic_connection_test() {
    char msg[6] = "hello";
    int err = send(client_socket, msg, 5, 0);
    if (err < 0) {
        teardown();
        printf("send() error: %s\n", strerror(errno));
    }
    ck_assert_int_eq(err, 5);

    char incoming[6];
    err = read(connection_socket, (void*)incoming, 5);
    if (err < 0) {
        teardown();
        printf("read() error: %s\n", strerror(errno));
    }
    ck_assert_int_eq(err, 5);
    ck_assert_str_eq(msg, incoming);
}

START_TEST(test_connect_to_server) {
    establish_connection();
    basic_connection_test();
}
END_TEST

START_TEST(test_connect_to_remote_server) {
    establish_remote_connection(55555);
    basic_connection_test();
}
END_TEST

START_TEST(test_disconnect_from_server) {
    establish_connection();
    disconnectFromServer(client_socket);
    int res = write(client_socket, "x", 1);
    if (res >= 0) {
        teardown();
        ck_assert_int_ge(res, 0);
    }
    if (errno != EBADF) {
        teardown();
        ck_assert_int_eq(EBADF, errno);
    }
    // Prevent the teardown function from trying to close the client
    // socket for a second time.
    client_socket = -1;
}
END_TEST

START_TEST(test_send_to_server) {
    establish_connection();
    int message_length;
    pthread_t tid;
    int err = pthread_create(&tid, NULL, read_from_client_detect_length, &message_length);
    if (err != 0) {
        printf("pthread_create() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }
    char* message = "hello, there";
    int expected_length = strlen(message);
    sendString(client_socket, message);
    char* from_client;
    pthread_join(tid, (void**)&from_client);
    if (err != 0) {
        printf("pthread_create() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }
    if (message_length != expected_length) {
        teardown();
        ck_assert_int_eq(expected_length, message_length);
    }
    if (strcmp(message, from_client) != 0) {
        teardown();
        ck_assert_str_eq(message, from_client);
    }
    free(from_client);
}
END_TEST

START_TEST(test_read_from_server) {
    establish_connection();
    char* message_to_client = "this is a message from the server, to the client!";
    pthread_t tid;
    int err = pthread_create(&tid, NULL, send_to_client, message_to_client);
    if (err != 0) {
        printf("pthread_create() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }

    uint32_t expected_length = strlen(message_to_client);
    uint32_t actual_length;
    char* from_server = readString(client_socket, &actual_length);
    err = pthread_join(tid, NULL);
    if (err != 0) {
        printf("pthread_create() failure: %s\n", strerror(errno));
        teardown();
        ck_assert_int_eq(0, err);
    }

    if (expected_length != actual_length) {
        teardown();
        ck_assert_int_eq(expected_length, actual_length);
    }
    if (strcmp(message_to_client, from_server) != 0) {
        teardown();
        ck_assert_str_eq(message_to_client, from_server);
    }

    free(from_server);
}
END_TEST

START_TEST(test_send_replacement_to_server) {
    establish_connection();
    uint32_t nchanges = 2;
    replacement_t* changes[nchanges];
    changes[0] = createIntReplacement("xyz", -65536);
    changes[1] = createDoubleReplacement("[Wheat].Path", -11400000.5);
    for (uint32_t i = 0; i < nchanges; i++) {
        test_send_replacement(changes[i]);
        replacement_free(changes[i]);
    }
}
END_TEST

START_TEST(test_run_with_changes) {
    establish_connection();

    replacement_t* change = createDoubleReplacement("xyz", 12.5);
    run_with_changes(&change, 1);
    replacement_free(change);

    change = createIntReplacement("", -65536);
    run_with_changes(&change, 1);
    replacement_free(change);

    int nchanges = 10;
    replacement_t** changes = malloc(nchanges * sizeof(replacement_t*));
    for (uint32_t i = 0; i < nchanges; i++) {
        char path[11];
        sprintf(path, "change[%u]", i);
        changes[i] = createIntReplacement(path, i);
    }
    run_with_changes(changes, nchanges);
    for (uint32_t i = 0; i < nchanges; i++) {
        replacement_free(changes[i]);
    }
}
END_TEST

Suite* client_test_suite() {
    Suite* suite;
    TCase* test_case;

    suite = suite_create("Client Tests");
    test_case = tcase_create("Client Test Case");

    tcase_add_test(test_case, test_connect_to_server);
    tcase_add_test(test_case, test_connect_to_remote_server);
    tcase_add_test(test_case, test_disconnect_from_server);
    tcase_add_test(test_case, test_send_to_server);
    tcase_add_test(test_case, test_read_from_server);
    tcase_add_test(test_case, test_send_replacement_to_server);
    tcase_add_test(test_case, test_run_with_changes);
    tcase_add_checked_fixture(test_case, setup, teardown);

    suite_add_tcase(suite, test_case);
    return suite;
}
