# SB.OpenSearch.Net

> **Experimental, AI-written rebuild. Not the official [OpenSearch.Net](https://www.nuget.org/packages/OpenSearch.Net) package. Not for production use.**

Low-level transport library for [OpenSearch](https://opensearch.org/), providing HTTP transport, node management, retry logic, and serialization infrastructure.

This is the transport layer that `SB.OpenSearch.Client` builds on. You typically install the client package instead for the full typed experience.

## What is this?

A ground-up rebuild of the OpenSearch .NET transport, written using AI (Claude) with inspiration from [opensearch-java](https://github.com/opensearch-project/opensearch-java) and [elasticsearch-net v8](https://github.com/elastic/elasticsearch-net). Built with System.Text.Json, targeting net8.0 and net10.0.

## Features

- HttpClient-based transport with handler rotation for DNS refresh
- Multi-node support with round-robin selection, dead-node tracking, and exponential backoff
- System.Text.Json serialization with async streaming
- Request audit trail for diagnostics
- AWS SigV4 authentication support

## Links

- [GitHub Repository](https://github.com/stefanobranco/opensearch-net)
- [Official OpenSearch .NET Client](https://github.com/opensearch-project/opensearch-net)
