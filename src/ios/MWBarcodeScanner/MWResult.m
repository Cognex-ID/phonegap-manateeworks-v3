//
//  MWResult.m
//  mobiscan_ALL
//
//  Created by vladimir zivkovic on 12/19/14.
//  Copyright (c) 2014 Manatee Works. All rights reserved.
//

#import "MWResult.h"
#import "BarcodeScanner.h"
//#import <CoreGraphics/CoreGraphics.h>

@implementation MWResults

+ (NSString *) getTypeName: (int) typeID {
    NSString *typeName = @"Unknown";
    switch (typeID) {
        case FOUND_25_INTERLEAVED: typeName = @"Code 25 Interleaved";break;
        case FOUND_25_STANDARD: typeName = @"Code 25 Standard";break;
        case FOUND_128: typeName = @"Code 128";break;
        case FOUND_128_GS1: typeName = @"Code 128 GS1";break;
        case FOUND_39: typeName = @"Code 39";break;
        case FOUND_93: typeName = @"Code 93";break;
        case FOUND_AZTEC: typeName = @"AZTEC";break;
        case FOUND_DM: typeName = @"Datamatrix";break;
        case FOUND_QR: typeName = @"QR";break;
        case FOUND_EAN_13: typeName = @"EAN 13";break;
        case FOUND_EAN_8: typeName = @"EAN 8";break;
        case FOUND_NONE: typeName = @"None";break;
        case FOUND_RSS_14: typeName = @"Databar 14";break;
        case FOUND_RSS_14_STACK: typeName = @"Databar 14 Stacked";break;
        case FOUND_RSS_EXP: typeName = @"Databar Expanded";break;
        case FOUND_RSS_LIM: typeName = @"Databar Limited";break;
        case FOUND_UPC_A: typeName = @"UPC A";break;
        case FOUND_UPC_E: typeName = @"UPC E";break;
        case FOUND_PDF: typeName = @"PDF417";break;
        case FOUND_CODABAR: typeName = @"Codabar";break;
        case FOUND_DOTCODE: typeName = @"Dotcode";break;
        case FOUND_11: typeName = @"Code 11";break;
        case FOUND_MSI: typeName = @"MSI Plessey";break;
        case FOUND_25_IATA: typeName = @"IATA Code 25";break;
        case FOUND_ITF14: typeName = @"ITF 14";break;
    }
    
    return typeName;
}

-(id)initWithBuffer:(uint8_t *)buffer
{
    self = [super init];
    if (self) {
        
        if (buffer[0] != 'M' || buffer[1] != 'W' || buffer[2] != 'R'){
            self = NULL;
            return NULL;
        }
        
        self.results = [[NSMutableArray alloc]init];
        self.count = 0;
        
        self.version = buffer[3];
        
        int count = buffer[4];
        
        int currentPos = 5;
        
        for (int i = 0; i < count; i++){
            
            MWResult *result = [[MWResult alloc] init];
            
            int fieldsCount = buffer[currentPos];
            currentPos++;
            for (int f = 0; f < fieldsCount; f++){
                int fieldType = buffer[currentPos];
                int fieldNameLength = buffer[currentPos + 1];
                int fieldContentLength = 256 * buffer[currentPos + 3 + fieldNameLength] + buffer[currentPos + 2 + fieldNameLength];
                NSString *fieldName = nil;
                
                if (fieldNameLength > 0){
                    fieldName = [[NSString alloc] initWithData:[NSData dataWithBytes:&buffer[currentPos + 2] length:fieldNameLength] encoding:NSUTF8StringEncoding];
                }
                
                int floatSize = sizeof(float);
                
                int contentPos = currentPos + fieldNameLength + 4;
                float locations[8];
                switch (fieldType) {
                    case MWB_RESULT_FT_TYPE:
                        result.type = *(uint32_t *)(&buffer[contentPos]);
                        result.typeName = [MWResults getTypeName:result.type];
                        break;
                    case MWB_RESULT_FT_SUBTYPE:
                        result.subtype = *(uint32_t *)(&buffer[contentPos]);
                        break;
                    case MWB_RESULT_FT_ISGS1:
                        result.isGS1 = (*(uint32_t *)(&buffer[contentPos]) == 1);
                        break;
                    case MWB_RESULT_FT_IMAGE_WIDTH:
                        result.imageWidth = *(uint32_t *)(&buffer[contentPos]);
                        break;
                    case MWB_RESULT_FT_IMAGE_HEIGHT:
                        result.imageHeight = *(uint32_t *)(&buffer[contentPos]);
                        break;
                        
                    case MWB_RESULT_FT_LOCATION:
                        for (int l = 0; l < 8; l++){
                            memcpy(&locations[l], &buffer[contentPos + l * 4], sizeof(float));
                           // locations[l] = *(float *)(&buffer[contentPos + l * 4]);
                        }
                        result.locationPoints = [[MWLocation alloc]initWithPoints:locations[0] y1:locations[1] x2:locations[2] y2:locations[3] x3:locations[4] y3:locations[5] x4:locations[6] y4:locations[7]];
                        break;
                    case MWB_RESULT_FT_TEXT:
                        result.text = [[NSString alloc] initWithData: [NSData dataWithBytes:&buffer[contentPos] length:fieldContentLength] encoding:NSUTF8StringEncoding];
                        break;
                    case MWB_RESULT_FT_BYTES:
                        result.bytes =     malloc(fieldContentLength);
                        result.bytesLength = fieldContentLength;
                        memcpy(result.bytes, &buffer[contentPos], fieldContentLength);

                        break;
                    case MWB_RESULT_FT_PARSER_BYTES:
                        result.encryptedResult =     malloc(fieldContentLength + 1);
                        result.encryptedResult[fieldContentLength] = 0;
                        memcpy(result.encryptedResult, &buffer[contentPos], fieldContentLength);
                        
                        break;

                        
                    default:
                        break;
                }
                
                currentPos += (fieldNameLength + fieldContentLength + 4);
                
            }
            
            
            [self.results addObject:result];
            
        }
        self.count = count;
        
        
        
    }
    return self;
}

- (MWResult *) resultAtIntex: (int) index {
    
    return [self.results objectAtIndex:index];
    
}


@end


@implementation MWResult

//-(id)initWithBuffer:(uint8_t *)buffer
//{
//    self = [super init];
//    if (self) {
//        
//    }
//    return self;
//}

@end


@implementation MWLocation


-(id)initWithPoints: (float) x1 y1: (float) y1 x2: (float) x2 y2: (float) y2 x3: (float) x3 y3: (float) y3 x4: (float) x4 y4: (float) y4
{
    self = [super init];
    if (self) {
        self.p1 = CGPointMake(x1, y1);
        self.p2 = CGPointMake(x2, y2);
        self.p3 = CGPointMake(x3, y3);
        self.p4 = CGPointMake(x4, y4);
        
        self.points[0] = CGPointMake(x1, y1);
        self.points[1] = CGPointMake(x2, y2);
        self.points[2] = CGPointMake(x3, y3);
        self.points[3] = CGPointMake(x4, y4);
        
    }
    return self;
}

- (CGPoint *)points
{
    return _points;
}

@end
