CSFILES=$(shell ls Pharaoh/*.cs 2> /dev/null)

all: libclient.so client.exe

submit: Main.class
	@echo "$(shell cd ..;sh submit.sh c)"


libclient.so: Library/*.cpp Library/*.h
	$(MAKE) -C Library/ libclient.so
	cp -f Library/libclient.so libclient.so

client.exe: $(CSFILES) libclient.so
	gmcs -out:client.exe  $(CSFILES)

clean:
	rm -f client.exe
	rm -f libclient.so
	make -C Library clean
