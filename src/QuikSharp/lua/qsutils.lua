--~ Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
--~ Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

local socket = require ("socket")
local json = require ("lua_cjson")
json.encode_keep_buffer(true)
json.encode_empty_table_as_object(false)
json.encode_numbers_as_base64(true);
json.decode_numbers_as_base64(true);

-- for callcacks, running in separate thread, use new instance
local json2 = json.new()
json2.encode_keep_buffer(true)
json2.encode_empty_table_as_object(false);
json2.encode_numbers_as_base64(true);

local qsutils = {}

function from_json(str)
    -- using lua_cjson
    local status, msg= pcall(json.decode, str)
    if status then
        return msg
    else
        return nil, msg
    end
end

function to_json(msg)
    local status, str= pcall(json.encode, msg)
    if status then
        return str
    else
        error(str)
    end
end

--- Sleep that always works
function delay(msec)
    if sleep then
        pcall(sleep, msec)
    else
        -- pcall(socket.select, nil, nil, msec / 1000)
    end
end

-- high precision current time
function timemsec()
    local st, res = pcall(socket.gettime)
    if st then
        return (res) * 1000
    else
        log("unexpected error in timemsec", 3)
        error("unexpected error in timemsec")
    end
end

--- Makes UniqueTransactionID ---
function getUniqueTransactionID(step)
	local trans_fname = script_path.."\\trans.id"
	local trans_id

	-- Пытается открыть файл в режиме "чтения/записи"
	trf = io.open(trans_fname,"r+")
	-- Если файл не существует
	if trf == nil then 
	  -- Создает файл в режиме "записи"
	  trf = io.open(trans_fname,"w")
	else
		trans_id = trf:read("*n")
	end
	if (trans_id == nil) or (trans_id <= 0) then
		trans_id = math.floor( math.fmod( timemsec()*100, 63244800 ) )
	end
	
	if step == nil then
		step = 1
	else 
		step = step + 0
		if step < 1 then
			step = 1
		end
	end

	trans_id = (trans_id + step) % 2147483647;
	if trans_id < step then
		trans_id = trans_id + step
	end
	
	trf:seek("set")
	trf:write(trans_id)
	trf:close();	
	return (trans_id - step + 1)
end


-- Returns the name of the file that calls this function (without extension)
function scriptFilename()
    -- Check that Lua runtime was built with debug information enabled
    if not debug or not debug.getinfo then
        return nil
    end
    local full_path = debug.getinfo(2, "S").source:sub(2)
    return string.gsub(full_path, "^.*\\(.*)[.]lua[c]?$", "%1")
end

-- when true will show QUIK message for log(...,0)
is_debug = false

-- log files
function openLog()
    os.execute("mkdir \""..script_path.."\\logs\"")
    local lf = io.open (script_path.. "\\logs\\QUIK#_"..os.date("%Y%m%d")..".log", "a")
    if not lf then
        lf = io.open (script_path.. "\\QUIK#_"..os.date("%Y%m%d")..".log", "a")
    end
    return lf
end

logfile = openLog()

-- closes log
function closeLog()
    if logfile then
        pcall(logfile:close(logfile))
    end
end

--- Write to log file and to Quik messages
function log(msg, level)
    if not msg then msg = "" end
    if level == 1 or level == 2 or level == 3 or is_debug then
        -- only warnings and recoverable errors to Quik
        if message then
            pcall(message, msg, level)
        end
    end
    if not level then level = 0 end
    local logLine = "LOG "..level..": "..msg
    print(logLine)
    local msecs = math.floor(math.fmod(timemsec(), 1000));
    if logfile then
        pcall(logfile.write, logfile, os.date("%Y-%m-%d %H:%M:%S").."."..msecs.." "..logLine.."\n")
        pcall(logfile.flush, logfile)
    end
end

-- Returns contents of config.json file or nil if no such file exists
function readConfigAsJson()
    local conf = io.open (script_path.. "\\config.json", "r")
    if not conf then
        return nil
    end
    local content = conf:read "*a"
    conf:close()
    return from_json(content)
end

function paramsFromConfig(scriptName)
    local params = {}
    -- just default values
    table.insert(params, "127.0.0.1") -- responseHostname
    table.insert(params, 34130)       -- responsePort
    table.insert(params, "127.0.0.1") -- callbackHostname
    table.insert(params, 34131)       -- callbackPort

    local config = readConfigAsJson()
    if not config or not config.servers then
        return nil
    end
    local found = false
    for i=1,#config.servers do
        local server = config.servers[i]
        if server.scriptName == scriptName then
            found = true
            if server.responseHostname then
                params[1] = server.responseHostname
            end
            if server.responsePort then
                params[2] = server.responsePort
            end
            if server.callbackHostname then
                params[3] = server.callbackHostname
            end
            if server.callbackPort then
                params[4] = server.callbackPort
            end
        end
    end

    if found then
        return params
    else
        return nil
    end
end

-- https://stackoverflow.com/questions/1426954/split-string-in-lua#comment73602874_7615129
function split2(inputstr, sep)
    sep = sep or '%s'
    local t = {}
    for field, s in string.gmatch(inputstr, "([^"..sep.."]*)("..sep.."?)") do
        table.insert(t, field)
        if s == "" then
            return t
        end
    end
end

-- current connection state
is_connected = false

-- we need two ports since callbacks and responses conflict and write to the same socket at the same time
-- I do not know how to make locking in Lua, it is just simpler to have two independent connections
-- To connect to a remote terminal - replace 'localhost' with the terminal ip-address

local response_server
local callback_server
local response_client
local callback_client

--- accept client on server
local function getResponseServer()
    log('Waiting for a response client...', 0)
	if not response_server then
		log("Cannot bind to response_server, probably the port is already in use", 3)
	else
		while true do
			local status, client, err = pcall(response_server.accept, response_server )
			if status and client then
				return client
			else
				log(err, 3)
			end
		end
	end
end

local function getCallbackClient()
    log('Waiting for a callback client...', 0)
	if not callback_server then
		log("Cannot bind to callback_server, probably the port is already in use", 3)
	else
		while true do
			local status, client, err = pcall(callback_server.accept, callback_server)
			if status and client then
				return client
			else
				log(err, 3)
			end
		end
	end
end

function qsutils.connect(response_host, response_port, callback_host, callback_port)
    if not response_server then
        response_server = socket.bind(response_host, response_port, 1)
    end
    if not callback_server then
        callback_server = socket.bind(callback_host, callback_port, 1)
    end

    if not is_connected then
        log('QUIK# is waiting for client connection...', 1)
        if response_client then
            log("is_connected is false but the response client is not nil", 3)
            -- Quik crashes without pcall
            pcall(response_client.close, response_client)
        end
        if callback_client then
            log("is_connected is false but the callback client is not nil", 3)
            -- Quik crashes without pcall
            pcall(callback_client.close, callback_client)
        end
        response_client = getResponseServer()
        callback_client = getCallbackClient()
        if response_client and callback_client then
            is_connected = true
            log('QUIK# client connected', 1)
        end
    end
end

local function disconnected()
    is_connected = false
    log('QUIK# client disconnected', 1)
    if response_client then
        pcall(response_client.close, response_client)
        response_client = nil
    end
    if callback_client then
        pcall(callback_client.close, callback_client)
        callback_client = nil
    end
    OnQuikSharpDisconnected()
end

--- get a decoded message as a table
function receiveRequest()
    if not is_connected then
        return nil, "not conencted"
    end
    local status, requestString = pcall(response_client.receive, response_client)
    if status and requestString then
        return requestString
    else
        disconnected()
        return nil, requestString
    end
end

function sendResponse(responseString)
    if is_connected then
        local status, res = pcall(response_client.send, response_client, responseString..'\n')
        if status and res then
            return true
        else
            disconnected()
            return nil, res
        end
    end
end

function sendCallback(msg_table)
    if not is_connected then
        return nil
    end

    -- if not set explicitly then set CreatedTime "t" property here
    if not msg_table.t then msg_table.t = timemsec() end

    local state, res, callback_string
    -- use another instance of json converter, cause callbacks runs in separate thread
    state, callback_string = pcall(json2.encode, msg_table)
    if not state then
        return nil, callback_string;
    end

    state, res = pcall(callback_client.send, callback_client, callback_string..'\n')
    if state and res then
        return true
    else
        disconnected()
        return nil, res
    end
end

function round(x)
  return x>=0 and math.floor(x+0.5) or math.ceil(x-0.5)
end

return qsutils
