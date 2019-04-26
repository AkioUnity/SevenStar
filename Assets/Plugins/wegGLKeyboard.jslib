mergeInto(LibraryManager.library, {
    focusHandleAction: function(_name, _str){
        console.log('focus:'+_name);
        if(UnityLoader.SystemInfo.mobile == true){
            console.log('focus _str:'+_str);
            var _inputTextData = prompt("Input Text", Pointer_stringify(_str));
            if (_inputTextData == null || _inputTextData == "") {
                //canceled text
            } else {
                //send data to unity
                SendMessage(Pointer_stringify(_name), 'ReceiveInputData', _inputTextData);
            }  
        }
    },
});