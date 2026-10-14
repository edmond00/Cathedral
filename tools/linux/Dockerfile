# The headless test suite in a container: Ubuntu 24.04, the packages cloud-setup.sh installs, the
# .NET 8 SDK, and the game built for Linux. The entry point runs the suite under Xvfb.
#
#   docker build -f tools/linux/Dockerfile -t cathedral-tests .
#   docker run --rm -v "$PWD/models:/src/models" cathedral-tests            # everything
#   docker run --rm -v "$PWD/models:/src/models" cathedral-tests audits     # or any run_tests.sh args
#
# The build context is the repository root (see .dockerignore). models/ is never copied in: it is
# mounted at run time. See tools/linux/README.md, including the Windows (Docker Desktop) form.

# Overridable for a registry mirror when Docker Hub rate-limits (429): the same image is
# mirror.gcr.io/library/ubuntu:24.04 or public.ecr.aws/ubuntu/ubuntu:24.04.
ARG BASE_IMAGE=ubuntu:24.04
FROM ${BASE_IMAGE}

ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    ALSOFT_DRIVERS=null

# Extra CA certificates for a TLS-intercepting proxy, if the build needs one: drop *.crt files in
# tools/linux/extra-ca/ (gitignored). Empty by default, and then this layer changes nothing.
COPY tools/linux/extra-ca/ /usr/local/share/ca-certificates/extra/
RUN apt-get update -qq && apt-get install -y -qq --no-install-recommends ca-certificates >/dev/null \
 && update-ca-certificates >/dev/null

WORKDIR /src

# One package list, in cloud-setup.sh. Copied alone first so the layer survives source edits; it
# --no-llama, so it reads nothing else from the repository.
COPY tools/linux/cloud-setup.sh tools/linux/cloud-setup.sh
RUN bash tools/linux/cloud-setup.sh --no-llama \
 && apt-get install -y -qq --no-install-recommends tini >/dev/null \
 && rm -rf /var/lib/apt/lists/*

# Restore before copying everything, so a source change does not re-download the packages.
COPY Cathedral.csproj Cathedral.sln ./
RUN dotnet restore Cathedral.csproj

COPY . .
RUN chmod +x run_tests.sh tools/linux/*.sh \
 && dotnet build -v q --nologo

# tini as PID 1: xvfb-run waits for Xvfb's SIGUSR1 to say the display is up, and as PID 1 itself it
# never sees it and hangs before running anything. tini also forwards `docker stop` to the suite,
# whose trap then takes the game processes down with it.
ENTRYPOINT ["/usr/bin/tini", "--", "tools/linux/docker-entrypoint.sh"]
