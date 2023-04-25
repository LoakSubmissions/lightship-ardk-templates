using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Niantic.Titan.Uploader.Internal {

  /// <summary>
  /// This logger is a simple implementation that replaces the original one
  /// in titan-uploader that uses platform.debugging library.
  /// </summary>
  [PublicAPI]
  internal class ChannelLogger {
    internal enum LogLevel
    {
      Off = 0,
      Fatal,
      Error,
      Warning,
      Info,
      Verbose,
      Trace
    }


    /// <summary>
    /// The name of the channel associated with events logged from this class
    /// </summary>
    public string ChannelName { get; }

    public ChannelLogger(string logChannel) {
      ChannelName = logChannel;
    }

    public LogLevel MaxLogLevel;

    /// <summary>
    /// Log a <see cref="LogLevel.Fatal"/> message to the log channel
    /// </summary>
    public void Fatal(string message) {
      if (MaxLogLevel != LogLevel.Off)
      {
        Debug.LogError(message);
      }
    }

    /// <summary>
    /// Log an <see cref="LogLevel.Error"/> message to the log channel
    /// </summary>
    public void Error(string message) {
      if (MaxLogLevel != LogLevel.Off && MaxLogLevel <= LogLevel.Error)
      {
        Debug.LogError(message);
      }
    }

    /// <summary>
    /// Log a <see cref="LogLevel.Warning"/> message to the log channel
    /// </summary>
    public void Warning(string message) {
      if (MaxLogLevel != LogLevel.Off && MaxLogLevel <= LogLevel.Warning)
      {
        Debug.LogWarning(message);
      }
    }

    /// <summary>
    /// Log an <see cref="LogLevel.Info"/> message to the log channel
    /// </summary>
    public void Info(string message)
    {
      if (MaxLogLevel != LogLevel.Off && MaxLogLevel <= LogLevel.Info)
      {
        Debug.Log(message);
      }
    }

    /// <summary>
    /// Log a <see cref="LogLevel.Verbose"/> message to the log channel
    /// </summary>
    public void Verbose(string message) {
      if (MaxLogLevel != LogLevel.Off && MaxLogLevel <= LogLevel.Verbose)
      {
        Debug.Log(message);
      }
    }

    /// <summary>
    /// Log a <see cref="LogLevel.Trace"/> message to the log channel
    /// </summary>
    public void LogTrace(string message) {
      if (MaxLogLevel == LogLevel.Trace)
      {
        Debug.Log(message);
      }
    }

    /// <summary>
    /// Log a message to the log channel
    /// </summary>
    /// <param name="logLevel">The message's severity</param>
    /// <param name="message">The message to log</param>
    public void LogMessage(LogLevel logLevel, string message) {
      if (MaxLogLevel != LogLevel.Off && MaxLogLevel <= logLevel)
      {
        if (logLevel == LogLevel.Error)
        {
          Error(message);
        } else if (logLevel == LogLevel.Warning)
        {
          Warning(message);
        }
        else
        {
          Debug.Log(message);
        }
      }
    }
  }
}