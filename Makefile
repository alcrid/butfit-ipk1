PROJECT_DIR=project1
RUNTIME=linux-x64
BUILD_DIR=$(PROJECT_DIR)/bin/Release/net8.0/$(RUNTIME)/publish
OUTPUT=ipk-l4-scan

.PHONY: all build move clean run

all: build move

build:
	dotnet publish $(PROJECT_DIR) -c Release -r $(RUNTIME) --self-contained true -p:PublishSingleFile=true -o $(BUILD_DIR)

move:
	cp $(BUILD_DIR)/project1 ./$(OUTPUT)
	chmod +x ./$(OUTPUT)

clean:
	rm -rf $(PROJECT_DIR)/bin $(PROJECT_DIR)/obj ./$(OUTPUT)

run: all
	sudo ./$(OUTPUT)
