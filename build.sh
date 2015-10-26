#!/usr/bin/env bash
#
# $1 is passed to package to enable deb or pkg packaging

$DIR/scripts/cmake-gen.sh
$DIR/scripts/bootstrap.sh
$DIR/scripts/package.sh $1
