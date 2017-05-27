﻿using Modbus.Core.Exceptions;
using System.Threading.Tasks;

namespace Modbus.Core
{
    public sealed class ModbusRtuSession : IModbusSession
    {
        private readonly IModbusProtocol _modbusProtocol;

        public ModbusRtuSession(IModbusProtocol modbusProtocol)
        {
            _modbusProtocol = modbusProtocol;
        }

        public Response<T> SendRequest<T>(int slaveAddress, int functionCode, object data) where T : struct
        {
            var builder = new RtuRequest.Builder()
                .SetSlaveAddress(slaveAddress)
                .SetFunctionCode(functionCode)
                .SetObject(data);

            return SendRequest<T>(builder);
        }

        public Response<T> SendRequest<T>(Request.BuilderBase builder) where T : struct
        {
            var request = builder.Build();

            var responseBytes =
                _modbusProtocol.SendForResult(request.RequestBytes, RtuResponse<T>.ComputeResponseBytesLength());

            if (responseBytes == null)
                return null;

            var response = new RtuResponse<T>.Builder()
                .SetResponseBytes(responseBytes)
                .Build();

            CheckResponse(request, responseBytes, response);

            return response;
        }

        public Task<Response<T>> SendRequestAsync<T>(int slaveAddress, int functionCode, object data) where T : struct
        {
            return Task.Run(() => SendRequest<T>(slaveAddress, functionCode, data));
        }

        public Task<Response<T>> SendRequestAsync<T>(Request.BuilderBase builder) where T : struct
        {
            return Task.Run(() => SendRequest<T>(builder));
        }

        private void CheckResponse<T>(Request request, byte[] responseBytes, Response<T> response)
            where T : struct
        {
            Checksum<T>(responseBytes);

            if (request.SlaveAddress != response.SlaveAddress)
                throw new MismatchDataException("Response slave address mismatch with " + request.SlaveAddress);
            if (request.FunctionCode != response.FunctionCode)
                throw new MismatchDataException("Response function code mismatch with " + request.FunctionCode);
        }

        private void Checksum<T>(byte[] responseBytes) where T : struct
        {
            var crc16 = Core.Checksum.ComputeCrc16(responseBytes, 0, responseBytes.Length - 2);
            if (crc16[0] != responseBytes[responseBytes.Length - 2] || crc16[1] != responseBytes[responseBytes.Length - 1])
                throw new DataCorruptedException("Checksum fail");
        }

        public void Dispose()
        {
            _modbusProtocol?.Dispose();
        }
    }
}