--~ Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
--~ Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

-- is running from Quik
function is_quik()
    if getScriptPath then return true else return false end
end

quikVersion = nil

script_path = "."

if is_quik() then
    script_path = getScriptPath()
    
	quikVersion = getInfoParam("VERSION")

	if quikVersion ~= nil then
		local t={}
		for str in string.gmatch(quikVersion, "([^%.]+)") do
			table.insert(t, str)
        end
		quikVersion = tonumber(t[1]) * 100 + tonumber(t[2])
	end

	if quikVersion == nil then
		message("QUIK# cannot detect QUIK version", 3)
		return
	else
		libPath = "\\clibs"
	end
    
    -- MD dynamic, requires MSVCRT
    -- MT static, MSVCRT is linked statically with luasocket
    -- package.cpath contains info.exe working directory, which has MSVCRT, so MT should not be needed in theory, 
    -- but in one issue someone said it doesn't work on machines that do not have Visual Studio. 
    local linkage = "MD"
    
	if quikVersion >= 805 then
        libPath = libPath .. "64\\53_"..linkage.."\\"
	elseif quikVersion >= 800 then
        libPath = libPath .. "64\\5.1_"..linkage.."\\"
	else
		libPath = "\\clibs\\5.1_"..linkage.."\\"
	end
end
package.path = package.path .. ";" .. script_path .. "\\?.lua;" .. script_path .. "\\?.luac"..";"..".\\?.lua;"..".\\?.luac"
package.cpath = package.cpath .. ";" .. script_path .. libPath .. '?.dll'..";".. '.' .. libPath .. '?.dll'

local util = require("qsutils")
local qf = require("qsfunctions")
local inspect = require ("inspect")

require("qscallbacks")

log("Detected Quik version: ".. quikVersion .." and using cpath: "..package.cpath  , 0)

local is_started = true

-- we need two ports since callbacks and responses conflict and write to the same socket at the same time
-- I do not know how to make locking in Lua, it is just simpler to have two independent connections
-- To connect to a remote terminal - replace '127.0.0.1' with the terminal ip-address
-- All this values could be replaced with values from config.json
local response_host = '127.0.0.1'
local response_port = 34130
local callback_host = '127.0.0.1'
local callback_port = response_port + 1

function do_main()
    log("Entered main function", 0)
    while is_started do
        -- if not connected, connect
        util.connect(response_host, response_port, callback_host, callback_port)
        -- when connected, process queue
        local t1, t2, t3, t4, t5        
        local requestMsg = nil
        local responseMsg = nil
        local err = nil

        -- receive message,
        local requestString = receiveRequest()
        t1 = timemsec()
        -- decode message ---
        if requestString then
            requestMsg, err = from_json(requestString)
            if not requestMsg then
                log("Failed decode from_json(requestString): " .. err, 3)
                log("   requestString: " .. inspect(requestString), 0)
            end
        end
        t2 = timemsec()
        if requestMsg then
            -- if ok, process message
            -- dispatch_and_process never throws, it returns lua errors wrapped as a message
            responseMsg, err = qf.dispatch_and_process(requestMsg)
            t3 = timemsec()
            if responseMsg then
                -- send message
                -- if not set explicitly then set CreatedTime "t" property here
                if not responseMsg.t then responseMsg.t = t3 end
                -- encode response ---
                local responseString = to_json(responseMsg)                
                t4 = timemsec()
                -- send response ---
                local res, err = sendResponse(responseString)
                t5 = timemsec()
                if not res then
                    log("Failed to sendResponse: ".. err, 3)
                end
            else
                log("Could not dispatch and process request: " .. err, 3)
                log(" requestMsg: " .. inspect(requestMsg), 0)
            end
        else
            delay(1)
        end
        -- log perfomance --
        if requestMsg then
            local total = round(t4 - t1)
            if total > 1 then
                local decode_time = round(t2 - t1)
                local dnp_time = round(t3 - t2)
                local encode_time = round(t4 - t3)
                local send_time = round(t5 - t4)
                local txt = "Perfomance for '" .. requestMsg.cmd .. "': " .. total .. "ms. Included: json.decode: " .. decode_time .. "ms., dispatch_and_process: " .. dnp_time .. "ms., json.encode: " .. encode_time .. "ms., send_response: " .. send_time .. "ms."
                log(txt, 0);
             end
        end
    end
end

function main()
    setup("QuikSharp")
    run()
end

--- catch errors
function run()
    local status, err = pcall(do_main)
    if status then
        log("finished")
    else
        log(err, 3)
    end
end

function setup(script_name)
    if not script_name then
        log("File name of this script is unknown. Please, set it explicity instead of scriptFilename() call inside your custom file", 3)
        return false
    end

    local list = paramsFromConfig(script_name)
    if list then
        response_host = list[1]
        response_port = list[2]
        callback_host = list[3]
        callback_port = list[4]
        printRunningMessage(script_name)
    elseif script_name == "QuikSharp" then
        -- use default values for this file in case no custom config found for it
        printRunningMessage(script_name)
    else -- do nothing when config is not found
        log("File config.json is not found or contains no entries for this script name: " .. script_name, 3)
        return false
    end

    return true
end

function printRunningMessage(script_name)
    log("Running from ".. script_name .. ", params: response " .. response_host .. ":" .. response_port ..", callback ".. " ".. callback_host ..":".. callback_port)
end

if not is_quik() then
    log("Hello, QUIK#! Running outside Quik.")
    setup("QuikSharp")
    do_main()
    logfile:close()
end

