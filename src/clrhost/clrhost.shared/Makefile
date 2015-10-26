CC := clang++
LD := clang++

OBJ := obj
BIN := bin

SOURCES := $(wildcard *.cpp)
OBJECTS := $(patsubst %.cpp,$(OBJ)/%.o,$(SOURCES))

CCARGS := -g

$(BIN)/clrhost: $(OBJECTS) | $(BIN)
	$(LD) $(OBJECTS) -o $@

$(OBJ)/%.o: %.cpp | $(OBJ)
	$(CC) $(CCARGS) -c $< -o $@ --std=c++11

$(BIN):
	@mkdir -p $@

$(OBJ):
	@mkdir -p $@

clean:
	@rm -Rf $(BIN)
	@rm -Rf $(OBJ)
